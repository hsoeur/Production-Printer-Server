using Api.PrinterProd.Configuration;
using Api.PrinterProd.Models;
using Api.PrinterProd.Services.api_data;
using CmdGenerator;
using DataLib;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Api.PrinterProd.Services.Labels
{
    public class LabelsCreator : ILabelsCreator
    {
        private readonly IApiData _apiData;
        private readonly IConfiguration _configuration;

        public LabelsCreator(IApiData apiData, IConfiguration configuration)
        {
            _apiData = apiData;
            _configuration = configuration;
        }

        public static string GetFirstThreeLettersOfSecondWordInUpperCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Split the input string by spaces
            var words = input.Split(' ');

            // Check if there are at least two words
            if (words.Length < 2)
                return string.Empty;

            // Get the second word
            var secondWord = words[1];

            // Get the first three letters of the second word and convert to uppercase
            var result = secondWord.Length >= 3 ? secondWord.Substring(0, 3).ToUpper() : secondWord.ToUpper();

            return result;
        }


       
        private string BuildAuthentiUid(string productUid, int serialNumber, string usagePrefix)
        {
            int prefixLength = _configuration.GetRequiredInt("Printing:AuthentiUidProductPrefixLength");
            string productPrefix = productUid.Length > prefixLength ? productUid.Substring(0, prefixLength) : productUid;
            return productPrefix + serialNumber + usagePrefix;
        }

        /// <summary>
        /// Conversion de la quantité de l'OF en entier.
        /// </summary>
        /// <param name="ordreFabrication"></param>
        /// <param name="isLabelInDb"></param>
        /// <param name="quantite"></param>
        private static void ParseQuantityOF(int quantity, out bool isLabelInDb, out int quantite)
        {
            isLabelInDb = false;
            quantite = quantity;
        }
        /// <summary>
        /// Lancement de la commande d'impression.
        /// </summary>
        /// <param name="usageCorrespondanceProduit"></param>
        private void ExecutePrinting(LabelTemplate usageCorrespondanceProduit, string outputFilePath)
        {
            string printer = _configuration["Printer:Share"];

            if (usageCorrespondanceProduit.LabelTypeUid != _configuration.GetRequiredString("Application:DefaultLotUsage"))
            {
                printer = _configuration["Printer:OtherThanLot"];
            }


            string user = _configuration.GetRequiredString("Printer:Samba:User");
            string password = _configuration.GetRequiredString("Printer:Samba:Password");

            // Conversion du chemin d'imprimante Windows pour Linux (Samba)
            string printerLinux = $"//{printer.Replace("\\\\", "").Replace("\\", "/")}";

            // Construction de la commande `smbclient`
            string commandBrady = $"echo \"{password}\" | smbclient {printerLinux} -U \"{user}\" -c \"put \\\"{outputFilePath}\\\"\"";

            Log.Information("CommandBrady : smbclient {PrinterLinux} -U {User} -c put {FilePath}", printerLinux, user, outputFilePath);

            try
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.StartInfo = startInfo;
                process.Start();

                process.StandardInput.WriteLine(commandBrady);
                process.StandardInput.Flush();
                process.StandardInput.Close();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                int exitCode = process.ExitCode;
                process.Close();

                Log.Information($"Sortie : {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    Log.Error($"Erreur : {error}");
                }

                Console.WriteLine($"Code de sortie : {exitCode}");
                
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de l'exécution de la commande : {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Méthode pour le remplissage du template d'impression.
        /// Et de l'impression, ainsi que de l'enregsitrement dans la base.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="uidOperateur"></param>
        /// <param name="ordreFabrication"></param>
        /// <param name="usageTemplate"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task<int> ProcessAndCreateLabelsCommon(DateTimeOffset date, string uidOperateur, ManufacturingOrder? ordreFabrication, LabelTemplate? usageTemplate, FirmwareVersion? version, string outputFilePath)
        {
            ArgumentNullException.ThrowIfNull(ordreFabrication);
            ArgumentNullException.ThrowIfNull(usageTemplate);
            ArgumentNullException.ThrowIfNull(version);

            var labels = await _apiData.GetCollectionLabelsByManufacturingOrderUid(ordreFabrication.ManufacturingOrderUid) ?? [];
            var usageLabels = labels.Where(label => label.LabelTypeUid == usageTemplate.LabelTypeUid).ToList();

            if (usageLabels.Count >= ordreFabrication.Quantity)
            {
                return -1;
            }

            var existingAuthentiUids = labels
                .Select(label => label.AuthentiUid)
                .ToHashSet(StringComparer.Ordinal);
            var labelsToCreate = new List<LabelDto>(ordreFabrication.Quantity - usageLabels.Count);
            var usagePrefix = GetFirstThreeLettersOfSecondWordInUpperCase(usageTemplate.LabelTypeUid);
            var firstSerialNumber = usageLabels.Count == 0
                ? ordreFabrication.FirstSerialNumber
                : usageLabels.Max(label => label.CurrentSerialNumber) + 1;
            var quantity = ordreFabrication.Quantity - usageLabels.Count;

            for (var serialNumber = firstSerialNumber; serialNumber < firstSerialNumber + quantity; serialNumber++)
            {
                var effectiveSerialNumber = ordreFabrication.FirstSerialNumber == 0 ? 0 : serialNumber;
                var label = BuildLabel(date, uidOperateur, ordreFabrication, usageTemplate, version, effectiveSerialNumber, usagePrefix);

                if (!existingAuthentiUids.Add(label.AuthentiUid))
                {
                    Log.Error("Label {AuthentiUid} already exists in the manufacturing order", label.AuthentiUid);
                    return -1;
                }

                labelsToCreate.Add(label);
            }

            await PersistLabelsAndPrint(labelsToCreate, usageTemplate, outputFilePath);
            return 0;
        }

        public async Task<int> ProcessAndCreateLabelsPartialPrint(DateTimeOffset date, string userUid, ManufacturingOrder ordreFabrication, LabelTemplate template, FirmwareVersion version, int printQuantity, string outputFilePath)
        {
            var labels = await _apiData.GetCollectionLabelsByManufacturingOrderUid(ordreFabrication.ManufacturingOrderUid) ?? [];
            var existingAuthentiUids = labels
                .Select(label => label.AuthentiUid)
                .ToHashSet(StringComparer.Ordinal);
            var labelsToCreate = new List<LabelDto>(printQuantity);
            var usagePrefix = GetFirstThreeLettersOfSecondWordInUpperCase(template.LabelTypeUid);

            for (var serialNumber = ordreFabrication.FirstSerialNumber; serialNumber < ordreFabrication.FirstSerialNumber + printQuantity; serialNumber++)
            {
                var label = BuildLabel(date, userUid, ordreFabrication, template, version, serialNumber, usagePrefix);

                if (!existingAuthentiUids.Add(label.AuthentiUid))
                {
                    Log.Error("Label {AuthentiUid} already exists in the manufacturing order", label.AuthentiUid);
                    return -1;
                }

                labelsToCreate.Add(label);
            }

            await PersistLabelsAndPrint(labelsToCreate, template, outputFilePath);
            return 0;
        }

        private LabelDto BuildLabel(
            DateTimeOffset date,
            string userUid,
            ManufacturingOrder manufacturingOrder,
            LabelTemplate template,
            FirmwareVersion version,
            int serialNumber,
            string usagePrefix) => new()
        {
            AuthentiUid = BuildAuthentiUid(manufacturingOrder.ProductUid, serialNumber, usagePrefix),
            UserUid = userUid,
            ManufacturingOrderUid = manufacturingOrder.ManufacturingOrderUid,
            CreationDate = date,
            CurrentSerialNumber = serialNumber,
            ProductVersion = version.Version,
            LabelTypeUid = template.LabelTypeUid,
            Template = template.TemplateFile,
            ProductUid = manufacturingOrder.ProductUid
        };

        private async Task PersistLabelsAndPrint(IReadOnlyCollection<LabelDto> labels, LabelTemplate template, string outputFilePath)
        {
            await _apiData.CreateEtiquettes(labels);
            Log.Information("Created {Count} labels in one batch for manufacturing order {ManufacturingOrderUid}",
                labels.Count,
                labels.First().ManufacturingOrderUid);
            ExecutePrinting(template, outputFilePath);
        }
    }
}
