using DataLib;
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
        public void ProcessTemplateCommon(ManufacturingOrder ordreFabrication, FirmwareVersion version, LabelTemplate usageCorrespondanceProduit, DateTimeOffset date, string uidOperateur, Article article, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:Path"];
            string templateContent = File.ReadAllText(pathToTemplateDir + usageCorrespondanceProduit.TemplateFile);

            int quantite = ordreFabrication.Quantity;
            int lastSerialNumber = quantite + ordreFabrication.FirstSerialNumber -2;
            int lastSerialNumberMontage = quantite + ordreFabrication.FirstSerialNumber -1;
            int lastSerialNumberEmballage = quantite + ordreFabrication.FirstSerialNumber -1;

            templateContent = templateContent.Replace("#SerialNumber#", lastSerialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberMontage#", lastSerialNumberMontage.ToString());
            templateContent = templateContent.Replace("#SerialNumberEmballage#", lastSerialNumberEmballage.ToString());
            templateContent = templateContent.Replace("#ReferenceTier#", article.SupplierReference);
            templateContent = templateContent.Replace("#UidProduitCut#", ordreFabrication.ProductUid.Substring(5).Replace("-", "").PadRight(6));
            templateContent = templateContent.Replace("#UidOF#", ordreFabrication.ManufacturingOrderUid.ToString());
            templateContent = templateContent.Replace("#UidOperateur#", uidOperateur);
            templateContent = templateContent.Replace("#Version#", version.Version);
            templateContent = templateContent.Replace("#UidProduit#", ordreFabrication.ProductUid);
            templateContent = templateContent.Replace("#Quantite#", ordreFabrication.Quantity.ToString("0"));

            // Use date.DateTime for formatting
            DateTime dateTime = date.DateTime;
            string formattedDateForLot = dateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

            templateContent = templateContent.Replace("#DD/MM/YY#", formattedDateForLot);
            templateContent = templateContent.Replace("#DD/MM/YYYY#", dateTime.ToString("dd/MM/yyyy"));
            templateContent = templateContent.Replace("#YYMMDD#", dateTime.ToString("yyMMdd"));
            templateContent = templateContent.Replace("#DD-MM-YYYY#", dateTime.ToString("dd-MM-yyyy"));

            templateContent = templateContent.Replace("#OffsetXLpt1#", configuration["LPT1:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt1#", configuration["LPT1:OffsetY"]);
            templateContent = templateContent.Replace("#OffsetXLpt2#", configuration["LPT2:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt2#", configuration["LPT2:OffsetY"]);

            File.WriteAllText("out_file.txt", templateContent);
        }

        public void ProcessTemplateInitials(string initials, string quantity, IConfiguration configuration)
        {
            try
            {
                // Chemin vers le template spécifique aux initiales
                string pathToTemplateDir = configuration["Template:InitialesDev"];
                string templateContent = File.ReadAllText(pathToTemplateDir);

                // Remplacement des balises dans le template
                templateContent = templateContent.Replace("#Initiales#", initials);
                templateContent = templateContent.Replace("#Quantite#", quantity);
                templateContent = templateContent.Replace("#DD/MM/YYYY#", DateTime.Now.ToString("dd/MM/yyyy"));

                // Écriture dans le fichier généré
                File.WriteAllText("out_file.txt", templateContent);

                Log.Information("Fichier d'impression des initiales généré avec succès.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de la génération du template pour les initiales.");
                throw;
            }
        }


        public void ProcessTemplatePartialPrint(ManufacturingOrder ordreFabrication, FirmwareVersion versionArticle, LabelTemplate usageCorrespondanceProduit, DateTimeOffset date, string userUid, Article article, int printQuantity, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:Path"];
            string templateContent = File.ReadAllText(pathToTemplateDir + usageCorrespondanceProduit.TemplateFile);

            int quantite = ordreFabrication.Quantity;
            int lastSerialNumber = quantite + ordreFabrication.FirstSerialNumber -2;
            int lastSerialNumberMontage = quantite + ordreFabrication.FirstSerialNumber -1;
            int lastSerialNumberEmballage = quantite + ordreFabrication.FirstSerialNumber -1;

            templateContent = templateContent.Replace("#SerialNumber#", lastSerialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberMontage#", lastSerialNumberMontage.ToString());
            templateContent = templateContent.Replace("#SerialNumberEmballage#", lastSerialNumberEmballage.ToString());

            templateContent = templateContent.Replace("#ReferenceTier#", article.SupplierReference);
            templateContent = templateContent.Replace("#UidProduitCut#", ordreFabrication.ProductUid.Substring(5).Replace("-", "").PadRight(6));
            templateContent = templateContent.Replace("#UidOF#", ordreFabrication.ManufacturingOrderUid.ToString());
            templateContent = templateContent.Replace("#UidOperateur#", userUid);
            templateContent = templateContent.Replace("#Version#", versionArticle.Version);
            templateContent = templateContent.Replace("#UidProduit#", ordreFabrication.ProductUid);
            templateContent = templateContent.Replace("#Quantite#", printQuantity.ToString("0"));

            // Correction: Use date.DateTime for formatting
            DateTime dateTime = date.DateTime;
            string formattedDateForLot = dateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

            templateContent = templateContent.Replace("#DD/MM/YY#", formattedDateForLot);
            templateContent = templateContent.Replace("#DD/MM/YYYY#", dateTime.ToString("dd/MM/yyyy"));
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
        public void ProcessTemplateReprint(Label etiquette, ManufacturingOrder ordre, LabelTemplate? usage, FirmwareVersion? version, Article article, IConfiguration configuration)
        {
            string pathToTemplateDir = configuration["Template:Path"];
            string templateContent = File.ReadAllText(pathToTemplateDir + usage.TemplateFile);

            int serialNumber = etiquette.CurrentSerialNumber -1;

            templateContent = templateContent.Replace("#SerialNumber#", serialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberMontage#", etiquette.CurrentSerialNumber.ToString());
            templateContent = templateContent.Replace("#SerialNumberEmballage#", etiquette.CurrentSerialNumber.ToString());
            templateContent = templateContent.Replace("#UidOF#", ordre.ManufacturingOrderUid.ToString());
            templateContent = templateContent.Replace("#UidOperateur#", etiquette.UserUid);
            templateContent = templateContent.Replace("#UidProduitCut#", ordre.ProductUid.Substring(5).Replace("-", "").PadRight(6));
            templateContent = templateContent.Replace("#Version#", version.Version);
            templateContent = templateContent.Replace("#UidProduit#", ordre.ProductUid);
            templateContent = templateContent.Replace("#Quantite#", "1");

            DateTime dateTime = etiquette.CreationDate.DateTime;
            string formattedDateForLot = dateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);
            templateContent = templateContent.Replace("#DD/MM/YY#", formattedDateForLot);
            templateContent = templateContent.Replace("#DD/MM/YYYY#", dateTime.ToString("dd/MM/yyyy"));
            templateContent = templateContent.Replace("#YYMMDD#", dateTime.ToString("yyMMdd"));
            templateContent = templateContent.Replace("#DD-MM-YYYY#", dateTime.ToString("dd-MM-yyyy"));

            templateContent = templateContent.Replace("#OffsetXLpt1#", configuration["LPT1:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt1#", configuration["LPT1:OffsetY"]);
            templateContent = templateContent.Replace("#OffsetXLpt2#", configuration["LPT2:OffsetX"]);
            templateContent = templateContent.Replace("#OffsetYLpt2#", configuration["LPT2:OffsetY"]);
            templateContent = templateContent.Replace("#ReferenceTier#", article.SupplierReference);

            File.WriteAllText("out_file.txt", templateContent);
        }
    }
}
