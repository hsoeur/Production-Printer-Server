using api_lo_etiq.Models;
using api_lo_etiq.Services.api_data;
using CmdGenerator;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System;
using System.Globalization;

namespace api_lo_etiq.Services.Labels
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


       
        /// <summary>
        /// Conversion de la quantité de l'OF en entier.
        /// </summary>
        /// <param name="ordreFabrication"></param>
        /// <param name="isLabelInDb"></param>
        /// <param name="quantite"></param>
        private static void ParseQuantityOF(string quantity, out bool isLabelInDb, out int quantite)
        {
            isLabelInDb = false;
            double quantiteDouble = double.Parse(quantity.Trim(), CultureInfo.InvariantCulture);
            quantite = (int)quantiteDouble;

        }
        /// <summary>
        /// Lancement de la commande d'impression.
        /// </summary>
        /// <param name="usageCorrespondanceProduit"></param>
        private void ExecutePrinting(Models.Template usageCorrespondanceProduit)
        {
            Log.Information("ScanViewModel::ExecutePrinting");

            string fileNameOutForBrady = "out_file.txt";
            string printer = _configuration["Printer:Share"];

            if (usageCorrespondanceProduit.Usage != "Etiquette Lot")
            {
                printer = _configuration["Printer:OtherThanLot"];
            }

            string commandBrady = $"copy \"{fileNameOutForBrady}\" \"{printer}\"";

            Log.Information($"CommandBrady : {commandBrady}");

            CommandGenerator commandGenerator = new CommandGenerator(commandBrady);
            commandGenerator.Execute();
            
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
        public async Task<int> ProcessAndCreateLabelsCommon(string date, string uidOperateur, ManufacturingOrder? ordreFabrication, Models.Template? usageTemplate, ArticleVersion? version)
        {
            int currentSN;
            if (!int.TryParse(ordreFabrication.FirstSerialNumber, out currentSN))
            {
                // Si FirstSerialNumber contient un point décimal, essayer de convertir en double puis en entier
                if (double.TryParse(ordreFabrication.FirstSerialNumber, out double parsedValue))
                {
                    currentSN = (int)Math.Floor(parsedValue); // Arrondi vers le bas si nécessaire
                }
                else
                {
                    throw new FormatException($"Invalid format for FirstSerialNumber: {ordreFabrication.FirstSerialNumber}");
                }
            }

            int ofQuantity;
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

            bool isLabelInDb = false;
            int quantite;
            string persoUsage = GetFirstThreeLettersOfSecondWordInUpperCase(usageTemplate.Usage);
            int ret = 0;

            List<Label> labels = await _apiData.GetCollectionLabelsByManufacturingOrderUid(ordreFabrication.ManufacturingOrderUid);
            
            if (labels.Count == ofQuantity && usageTemplate.Usage == labels.First().Usage)
            {
                ret = -1;
            }
            else if (labels.Count < ofQuantity && labels.Count != 0)
            {
                int remainingQuantity = ofQuantity - labels.Count;
                Label label = await _apiData.GetLabelLastSerialNumberPrinted(ordreFabrication.ManufacturingOrderUid);
                int currentSerialNumber = int.Parse(label.CurrentSerialNumber)+1;

                for (int i = currentSerialNumber; i < currentSerialNumber + remainingQuantity; i++)
                {
                    string currentSnString = i.ToString();

                    if (ordreFabrication.FirstSerialNumber == "0")
                    {
                        currentSnString = "0";
                        LabelDto etiWithoutSN = new LabelDto
                        {
                            AuthentiUid = ordreFabrication.ProductUid.Substring(0, 9) + currentSnString + persoUsage,
                            UserUid = uidOperateur,
                            ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                            CreationDate = date,
                            CurrentSerialNumber = currentSnString,
                            ProductVersion = version.FirmwareVersion,
                            Usage = usageTemplate.Usage,
                            Template = usageTemplate.TemplateFile,
                            ProductUid = ordreFabrication.ProductUid
                        };

                        _apiData.CreateEtiquette(etiWithoutSN);
                        Log.Debug($"Création d'une étiquette sans numéro de série: {etiWithoutSN.AuthentiUid}, {etiWithoutSN.UserUid}, {etiWithoutSN.ManufacturingOrderUid}, {etiWithoutSN.CreationDate}, {etiWithoutSN.CurrentSerialNumber}, {etiWithoutSN.ProductUid}, {etiWithoutSN.Usage}");
                    }
                    else
                    {
                        LabelDto etiDto = new LabelDto
                        {
                            AuthentiUid = ordreFabrication.ProductUid.Substring(0, 9) + currentSnString + persoUsage,
                            UserUid = uidOperateur,
                            ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                            CreationDate = date,
                            CurrentSerialNumber = currentSnString,
                            ProductVersion = version.FirmwareVersion,
                            Usage = usageTemplate.Usage,
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
                            Log.Debug($"Création d'une étiquette : {etiDto.AuthentiUid}, {etiDto.UserUid}, {etiDto.ManufacturingOrderUid}, {etiDto.CreationDate}, {etiDto.CurrentSerialNumber}, {etiDto.ProductVersion}, {etiDto.Usage}");
                        }
                    }
                }

                if (!isLabelInDb)
                {
                    ExecutePrinting(usageTemplate);
                }

                return 0;

            }
            else
            {
                ret = NominalLabelCreation(date, uidOperateur, ordreFabrication, usageTemplate, version, currentSN, out isLabelInDb, out quantite, persoUsage);
            }

            return ret;
        }


        private int NominalLabelCreation(string date, string uidOperateur, ManufacturingOrder? ordreFabrication, Models.Template? usageTemplate, ArticleVersion? version, int currentSN, out bool isLabelInDb, out int quantite, string persoUsage)
        {
            ParseQuantityOF(ordreFabrication.Quantity, out isLabelInDb, out quantite);

            for (int i = currentSN; i < currentSN + quantite; i++)
            {
                string currentSnString = i.ToString();

                if (ordreFabrication.FirstSerialNumber == "0")
                {
                    currentSnString = "0";
                    LabelDto etiWithoutSN = new LabelDto
                    {
                        AuthentiUid = ordreFabrication.ProductUid.Substring(0, 9) + currentSnString + persoUsage,
                        UserUid = uidOperateur,
                        ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                        CreationDate = date,
                        CurrentSerialNumber = currentSnString,
                        ProductVersion = version.FirmwareVersion,
                        Usage = usageTemplate.Usage,
                        Template = usageTemplate.TemplateFile,
                        ProductUid = ordreFabrication.ProductUid
                    };

                    _apiData.CreateEtiquette(etiWithoutSN);
                    Log.Debug($"Création d'une étiquette sans numéro de série: {etiWithoutSN.AuthentiUid}, {etiWithoutSN.UserUid}, {etiWithoutSN.ManufacturingOrderUid}, {etiWithoutSN.CreationDate}, {etiWithoutSN.CurrentSerialNumber}, {etiWithoutSN.ProductUid}, {etiWithoutSN.Usage}");
                }
                else
                {
                    LabelDto etiDto = new LabelDto
                    {
                        AuthentiUid = ordreFabrication.ProductUid.Substring(0, 9) + currentSnString + persoUsage,
                        UserUid = uidOperateur,
                        ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                        CreationDate = date,
                        CurrentSerialNumber = currentSnString,
                        ProductVersion = version.FirmwareVersion,
                        Usage = usageTemplate.Usage,
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
                        Log.Debug($"Création d'une étiquette : {etiDto.AuthentiUid}, {etiDto.UserUid}, {etiDto.ManufacturingOrderUid}, {etiDto.CreationDate}, {etiDto.CurrentSerialNumber}, {etiDto.ProductVersion}, {etiDto.Usage}");
                    }
                }
            }

            if (!isLabelInDb)
            {
                ExecutePrinting(usageTemplate);
            }

            return 0;
        }

        public int ProcessAndCreateLabelsPartialPrint(string date, string userUid, ManufacturingOrder ordreFabrication, Models.Template template, ArticleVersion version, string printQuantity)
        {
            try
            {
                int currentSN = int.Parse(ordreFabrication.FirstSerialNumber);
                bool isLabelInDb;
                int quantite;
                string persoUsage = GetFirstThreeLettersOfSecondWordInUpperCase(template.Usage);

                ParseQuantityOF(printQuantity, out isLabelInDb, out quantite);

                for (int i = currentSN; i < currentSN + quantite; i++)
                {

                    string currentSnString = i.ToString();

                    LabelDto etiDto = new LabelDto
                    {
                        AuthentiUid = ordreFabrication.ProductUid.Substring(0, 9) + currentSnString + persoUsage,
                        UserUid = userUid,
                        ManufacturingOrderUid = ordreFabrication.ManufacturingOrderUid,
                        CreationDate = date,
                        CurrentSerialNumber = currentSnString,
                        ProductVersion = version.FirmwareVersion,
                        Usage = template.Usage,
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
                        Log.Debug($"Création d'une étiquette : {etiDto.AuthentiUid}, {etiDto.UserUid}, {etiDto.ManufacturingOrderUid}, {etiDto.CreationDate}, {etiDto.CurrentSerialNumber}, {etiDto.ProductVersion}, {etiDto.Usage}");
                    }
                }

                if (!isLabelInDb)
                {
                    ExecutePrinting(template);
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
