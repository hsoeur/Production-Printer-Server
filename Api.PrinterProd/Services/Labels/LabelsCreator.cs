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
            int currentSN = ordreFabrication.FirstSerialNumber;

            /*
            if (!int.TryParse(ordreFabrication.Quantity.Trim(), out ofQuantity))
            {
                // Si Quantity contient un point décimal, essayer de convertir en double puis en entier
                if (double.TryParse(ordreFabrication.Quantity.Trim(), out double parsedValue))
                {
                    ofQuantity = (int)Math.Floor(parsedValue); // Arrondi vers le bas si nécessaire
                }
                else
                {
                    throw new FormatException($"Invalid format for Quantity: {ordreFabrication.Quantity}");
                }
            }
            */

            bool isLabelInDb = false;
            int quantite;
            string persoUsage = GetFirstThreeLettersOfSecondWordInUpperCase(usageTemplate.LabelTypeUid);
            int ret =0;

            List<Label> labels = await _apiData.GetCollectionLabelsByManufacturingOrderUid(ordreFabrication.ManufacturingOrderUid);

            if(labels == null)
            {
                labels = new List<Label>();
            }
            
            if (labels.Count == ordreFabrication.Quantity && usageTemplate.LabelTemplateUid == labels.First().LabelTypeUid)
            {
                ret = -1;
            }

            else if (labels.Count < ordreFabrication.Quantity && labels.Count !=0)
            {
                int remainingQuantity = ordreFabrication.Quantity - labels.Count;
                Label label = await _apiData.GetLabelLastSerialNumberPrinted(ordreFabrication.ManufacturingOrderUid);
                
                int currentSerialNumber = label.CurrentSerialNumber +1;

                for (int i = currentSerialNumber; i < currentSerialNumber + remainingQuantity; i++)
                {
                    int currentSnString = i;

                    if (ordreFabrication.FirstSerialNumber ==0)
                    {
                        currentSnString = 0;
                        LabelDto etiWithoutSN = new LabelDto
                        {
                            AuthentiUid = BuildAuthentiUid(ordreFabrication.ProductUid, currentSnString, persoUsage),
                            UserUid = uidOperateur,
                            ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                            CreationDate = date,
                            CurrentSerialNumber = currentSnString,
                            ProductVersion = version.Version,
                            LabelTypeUid = usageTemplate.LabelTypeUid,
                            Template = usageTemplate.TemplateFile,
                            ProductUid = ordreFabrication.ProductUid
                        };

                        _apiData.CreateEtiquette(etiWithoutSN);
                        Log.Debug($"Création d'une étiquette sans numéro de série: {etiWithoutSN.AuthentiUid}, {etiWithoutSN.UserUid}, {etiWithoutSN.ManufacturingOrderUid}, {etiWithoutSN.CreationDate}, {etiWithoutSN.CurrentSerialNumber}, {etiWithoutSN.ProductUid}, {etiWithoutSN.LabelTypeUid}");
                    }
                    else
                    {
                        LabelDto etiDto = new LabelDto
                        {
                            AuthentiUid = BuildAuthentiUid(ordreFabrication.ProductUid, currentSnString, persoUsage),
                            UserUid = uidOperateur,
                            ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                            CreationDate = date,
                            CurrentSerialNumber = currentSnString,
                            ProductVersion = version.Version,
                            LabelTypeUid = usageTemplate.LabelTypeUid,
                            Template = usageTemplate.TemplateFile,
                            ProductUid = ordreFabrication.ProductUid
                        };

                        isLabelInDb = _apiData.EtiquetteAlreadyExists(etiDto.AuthentiUid).Result;

                        if (isLabelInDb)
                        {
                            Log.Error($"ScanViewModel::PrintLabels() -- {etiDto.AuthentiUid} existe déjà dans la base.");
                            return -1;
                        }
                        else
                        {
                            _apiData.CreateEtiquette(etiDto);
                            Log.Debug($"Création d'une étiquette : {etiDto.AuthentiUid}, {etiDto.UserUid}, {etiDto.ManufacturingOrderUid}, {etiDto.CreationDate}, {etiDto.CurrentSerialNumber}, {etiDto.ProductVersion}, {etiDto.LabelTypeUid}");
                        }
                    }
                }

                if (!isLabelInDb)
                {
                    ExecutePrinting(usageTemplate, outputFilePath);
                }

                return 0;

            }
            else
            {
                ret = NominalLabelCreation(date, uidOperateur, ordreFabrication, usageTemplate, version, currentSN, out isLabelInDb, out quantite, persoUsage, outputFilePath);
            }

            return ret;
        }



        private int NominalLabelCreation(DateTimeOffset date, string uidOperateur, ManufacturingOrder? ordreFabrication, LabelTemplate? usageTemplate, FirmwareVersion? version, int currentSN, out bool isLabelInDb, out int quantite, string persoUsage, string outputFilePath)
        {
            ParseQuantityOF(ordreFabrication.Quantity, out isLabelInDb, out quantite);

            for (int i = currentSN; i < currentSN + quantite; i++)
            {
                int currentSnString = i;

                if (ordreFabrication.FirstSerialNumber ==0)
                {
                    currentSnString = 0;
                    LabelDto etiWithoutSN = new LabelDto
                    {
                        AuthentiUid = BuildAuthentiUid(ordreFabrication.ProductUid, currentSnString, persoUsage),
                        UserUid = uidOperateur,
                        ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                        CreationDate = date,
                        CurrentSerialNumber = currentSnString,
                        ProductVersion = version.Version,
                        LabelTypeUid = usageTemplate.LabelTypeUid,
                        Template = usageTemplate.TemplateFile,
                        ProductUid = ordreFabrication.ProductUid
                    };

                    _apiData.CreateEtiquette(etiWithoutSN);
                    Log.Debug($"Création d'une étiquette sans numéro de série: {etiWithoutSN.AuthentiUid}, {etiWithoutSN.UserUid}, {etiWithoutSN.ManufacturingOrderUid}, {etiWithoutSN.CreationDate}, {etiWithoutSN.CurrentSerialNumber}, {etiWithoutSN.ProductUid}, {etiWithoutSN.LabelTypeUid}");
                }
                else
                {
                    LabelDto etiDto = new LabelDto
                    {
                        AuthentiUid = BuildAuthentiUid(ordreFabrication.ProductUid, currentSnString, persoUsage),
                        UserUid = uidOperateur,
                        ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                        CreationDate = date,
                        CurrentSerialNumber = currentSnString,
                        ProductVersion = version.Version,
                        LabelTypeUid = usageTemplate.LabelTypeUid,
                        Template = usageTemplate.TemplateFile,
                        ProductUid = ordreFabrication.ProductUid
                    };

                    isLabelInDb = _apiData.EtiquetteAlreadyExists(etiDto.AuthentiUid).Result;

                    if (isLabelInDb)
                    {
                        Log.Error($"ScanViewModel::PrintLabels() -- {etiDto.AuthentiUid} existe déjà dans la base.");
                        return -1;
                    }
                    else
                    {
                        _apiData.CreateEtiquette(etiDto);
                        Log.Debug($"Création d'une étiquette : {etiDto.AuthentiUid}, {etiDto.UserUid}, {etiDto.ManufacturingOrderUid}, {etiDto.CreationDate}, {etiDto.CurrentSerialNumber}, {etiDto.ProductVersion}, {etiDto.LabelTypeUid}");
                    }
                }
            }

            if (!isLabelInDb)
            {
                ExecutePrinting(usageTemplate, outputFilePath);
            }

            return 0;
        }

        public int ProcessAndCreateLabelsPartialPrint(DateTimeOffset date, string userUid, ManufacturingOrder ordreFabrication, LabelTemplate template, FirmwareVersion version, int printQuantity, string outputFilePath)
        {
            try
            {
                int currentSN = ordreFabrication.FirstSerialNumber;
                bool isLabelInDb;
                int quantite;
                string persoUsage = GetFirstThreeLettersOfSecondWordInUpperCase(template.LabelTypeUid);

                ParseQuantityOF(printQuantity, out isLabelInDb, out quantite);

                for (int i = currentSN; i < currentSN + quantite; i++)
                {

                    int currentSnString = i;

                    LabelDto etiDto = new LabelDto
                    {
                        AuthentiUid = BuildAuthentiUid(ordreFabrication.ProductUid, currentSnString, persoUsage),
                        UserUid = userUid,
                        ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                        CreationDate = date,
                        CurrentSerialNumber = currentSnString,
                        ProductVersion = version.Version,
                        LabelTypeUid = template.LabelTypeUid,
                        Template = template.TemplateFile,
                        ProductUid = ordreFabrication.ProductUid
                    };

                    isLabelInDb = _apiData.EtiquetteAlreadyExists(etiDto.AuthentiUid).Result;

                    if (isLabelInDb)
                    {
                        Log.Error($"ScanViewModel::PrintLabels() -- {etiDto.AuthentiUid} existe déjà dans la base.");
                        return -1;
                    }
                    else
                    {
                        _apiData.CreateEtiquette(etiDto);
                        Log.Debug($"Création d'une étiquette : {etiDto.AuthentiUid}, {etiDto.UserUid}, {etiDto.ManufacturingOrderUid}, {etiDto.CreationDate}, {etiDto.CurrentSerialNumber}, {etiDto.ProductVersion}, {etiDto.LabelTypeUid}");
                    }
                }

                if (!isLabelInDb)
                {
                    ExecutePrinting(template, outputFilePath);
                }

                return 0;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
