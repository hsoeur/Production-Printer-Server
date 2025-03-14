using api_lo_etiq.Models;
using Serilog;
using System;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace api_lo_etiq.Services.Template
{
    public class LabelTemplateProcessor : ILabelTemplateProcessor
    {
        public void ProcessTemplateArtBorne(Article article, string quantity, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:PathArtBorne"];

            string templateContent = File.ReadAllText(pathToTemplateDir);


            templateContent = templateContent.Replace("#DD/MM/YYYY#", DateTime.Now.ToString("dd/MM/yyyy"));
            templateContent = templateContent.Replace("#CodeArticleHorizon#", article.SupplierReference);
            templateContent = templateContent.Replace("#Quantite#", quantity);
            File.WriteAllText("out_file_art_borne.txt", templateContent);

        }

        /// <summary>
        /// Méthode de parsage du fichier template pour l'impression d'étiquettes de production.
        /// </summary>
        /// <param name="ordreFabrication"></param>
        /// <param name="version"></param>
        /// <param name="usageCorrespondanceProduit"></param>
        /// <param name="date"></param>
        /// <param name="uidOperateur"></param>
        /// <param name="configuration"></param>
        public void ProcessTemplateCommon(ManufacturingOrder ordreFabrication, ArticleVersion version, Models.Template usageCorrespondanceProduit, string date, string uidOperateur, Article article, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:Path"];

            Log.Information($"PathToTemplateDirectory : {pathToTemplateDir}");

            Log.Information($"FileTemplate : {pathToTemplateDir + usageCorrespondanceProduit.TemplateFile}");

            string templateContent = File.ReadAllText(pathToTemplateDir + usageCorrespondanceProduit.TemplateFile);

            double quantiteDouble = double.Parse(ordreFabrication.Quantity.Trim(), CultureInfo.InvariantCulture);

            int quantite = (int)quantiteDouble;

            int lastSerialNumber = quantite + int.Parse(ordreFabrication.FirstSerialNumber) - 2;
            int lastSerialNumberMontage = quantite + int.Parse(ordreFabrication.FirstSerialNumber) - 1;
            int lastSerialNumberEmballage = quantite + int.Parse(ordreFabrication.FirstSerialNumber) - 1;

            templateContent = templateContent.Replace("#SerialNumber#", lastSerialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberMontage#", lastSerialNumberMontage.ToString());
            templateContent = templateContent.Replace("#SerialNumberEmballage#", lastSerialNumberEmballage.ToString());

            templateContent = templateContent.Replace("#ReferenceTier#", article.SupplierReference);

            templateContent = templateContent.Replace("#UidProduitCut#", ordreFabrication.ProductUid.Substring(5).Replace("-", "").PadRight(6));
            templateContent = templateContent.Replace("#UidOF#", ordreFabrication.ManufacturingOrderUid);

            templateContent = templateContent.Replace("#UidOperateur#", uidOperateur);

            templateContent = templateContent.Replace("#Version#", version.FirmwareVersion);
            templateContent = templateContent.Replace("#UidProduit#", ordreFabrication.ProductUid);
            templateContent = templateContent.Replace("#Quantite#", decimal.Parse(ordreFabrication.Quantity).ToString("0"));

            DateTime dateTime = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string formattedDateForLot = dateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

            templateContent = templateContent.Replace("#DD/MM/YY#", formattedDateForLot);
            templateContent = templateContent.Replace("#DD/MM/YYYY#", date);
            templateContent = templateContent.Replace("#YYMMDD#", dateTime.ToString("yyMMdd"));
            templateContent = templateContent.Replace("#DD-MM-YYYY#", dateTime.ToString("dd-MM-yyyy"));

            templateContent = templateContent.Replace("#OffsetXLpt1#", configuration["LPT1:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt1#", configuration["LPT1:OffsetY"]);
            templateContent = templateContent.Replace("#OffsetXLpt2#", configuration["LPT2:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt2#", configuration["LPT2:OffsetY"]);

            File.WriteAllText("out_file.txt", templateContent);
        }

        public void ProcessTemplatePartialPrint(ManufacturingOrder ordreFabrication, ArticleVersion versionArticle, Models.Template usageCorrespondanceProduit, string date, string userUid, Article article, string printQuantity, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:Path"];

            Log.Information($"PathToTemplateDirectory : {pathToTemplateDir}");

            Log.Information($"FileTemplate : {pathToTemplateDir + usageCorrespondanceProduit.TemplateFile}");

            string templateContent = File.ReadAllText(pathToTemplateDir + usageCorrespondanceProduit.TemplateFile);

            double quantiteDouble = double.Parse(ordreFabrication.Quantity.Trim(), CultureInfo.InvariantCulture);

            int quantite = (int)quantiteDouble;

            int lastSerialNumber = quantite + int.Parse(ordreFabrication.FirstSerialNumber) - 2;
            int lastSerialNumberMontage = quantite + int.Parse(ordreFabrication.FirstSerialNumber) - 1;
            int lastSerialNumberEmballage = quantite + int.Parse(ordreFabrication.FirstSerialNumber) - 1;

            templateContent = templateContent.Replace("#SerialNumber#", lastSerialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberMontage#", lastSerialNumberMontage.ToString());
            templateContent = templateContent.Replace("#SerialNumberEmballage#", lastSerialNumberEmballage.ToString());

            templateContent = templateContent.Replace("#ReferenceTier#", article.SupplierReference);

            templateContent = templateContent.Replace("#UidProduitCut#", ordreFabrication.ProductUid.Substring(5).Replace("-", "").PadRight(6));
            templateContent = templateContent.Replace("#UidOF#", ordreFabrication.ManufacturingOrderUid);

            templateContent = templateContent.Replace("#UidOperateur#", userUid);

            templateContent = templateContent.Replace("#Version#", versionArticle.FirmwareVersion);
            templateContent = templateContent.Replace("#UidProduit#", ordreFabrication.ProductUid);
            templateContent = templateContent.Replace("#Quantite#", decimal.Parse(printQuantity).ToString("0"));

            DateTime dateTime = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string formattedDateForLot = dateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

            templateContent = templateContent.Replace("#DD/MM/YY#", formattedDateForLot);
            templateContent = templateContent.Replace("#DD/MM/YYYY#", date);
            templateContent = templateContent.Replace("#YYMMDD#", dateTime.ToString("yyMMdd"));
            templateContent = templateContent.Replace("#DD-MM-YYYY#", dateTime.ToString("dd-MM-yyyy"));

            templateContent = templateContent.Replace("#OffsetXLpt1#", configuration["LPT1:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt1#", configuration["LPT1:OffsetY"]);
            templateContent = templateContent.Replace("#OffsetXLpt2#", configuration["LPT2:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt2#", configuration["LPT2:OffsetY"]);

            File.WriteAllText("out_file.txt", templateContent);
        }

      
        /// <summary>
        /// Méthode de parsage du fichier template pour la réimpression d'une étiquette.
        /// </summary>
        /// <param name="etiquette"></param>
        /// <param name="ordre"></param>
        /// <param name="usage"></param>
        /// <param name="version"></param>
        /// <param name="configuration"></param>
        public void ProcessTemplateReprint(Label etiquette, ManufacturingOrder ordre, Models.Template? usage, ArticleVersion? version, Article article, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:Path"];
            Log.Information($"PathToTemplateDirectory : {pathToTemplateDir}");

            Log.Information($"FileTemplate : {pathToTemplateDir + usage.TemplateFile}");

            string templateContent = File.ReadAllText(pathToTemplateDir + usage.TemplateFile);

            Log.Information("Template Content : " + templateContent);

            int serialNumber = int.Parse(etiquette.CurrentSerialNumber) - 1;

            Log.Debug("Serial Number => " + serialNumber);

            templateContent = templateContent.Replace("#SerialNumber#", serialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberMontage#", etiquette.CurrentSerialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberEmballage#", etiquette.CurrentSerialNumber.ToString());
            templateContent = templateContent.Replace("#UidOF#", ordre.ManufacturingOrderUid);
            templateContent = templateContent.Replace("#UidOperateur#", etiquette.UserUid);
            templateContent = templateContent.Replace("#UidProduitCut#", ordre.ProductUid.Substring(5).Replace("-", "").PadRight(6));
            templateContent = templateContent.Replace("#Version#", version.FirmwareVersion);
            templateContent = templateContent.Replace("#UidProduit#", ordre.ProductUid);
            templateContent = templateContent.Replace("#Quantite#", "1");

            Log.Debug("Date Time => "+ etiquette.CreationDate);

            DateTime dateTime = DateTime.ParseExact(etiquette.CreationDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            string formattedDateForLot = dateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

            templateContent = templateContent.Replace("#DD/MM/YY#", formattedDateForLot);
            templateContent = templateContent.Replace("#DD/MM/YYYY#", etiquette.CreationDate);
            templateContent = templateContent.Replace("#YYMMDD#", dateTime.ToString("yyMMdd"));
            templateContent = templateContent.Replace("#DD-MM-YYYY#", dateTime.ToString("dd-MM-yyyy"));

            templateContent = templateContent.Replace("#OffsetXLpt1#", configuration["LPT1:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt1#", configuration["LPT1:OffsetY"]);
            templateContent = templateContent.Replace("#OffsetXLpt2#", configuration["LPT2:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt2#", configuration["LPT2:OffsetY"]);

            templateContent = templateContent.Replace("#ReferenceTier#", article.SupplierReference);

            File.WriteAllText("out_file.txt", templateContent);
            Log.Debug("Fichier d'impression généré avec succès.");
            
        }
    }
}
