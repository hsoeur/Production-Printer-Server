using DataLib;
using Microsoft.Extensions.Configuration;

namespace api_lo_etiq.Services.Template
{
    public interface ILabelTemplateProcessor
    {
        /*
        void ProcessTemplateAccessoire(OrdreFabrication ordreFabrication, UsageCorrespondanceProduit usageCorrespondanceProduit, string uidOperateur, string date, IConfiguration configuration);
        void ProcessTemplateEmballage(OrdreFabrication ordreFabrication, UsageCorrespondanceProduit usage, string uidOperateur, string date, IConfiguration configuration);

        /// <summary>
        /// Remplir les templates pour l'impression normale.
        /// </summary>
        /// <param name="ordreFabrication"></param>
        /// <param name="article"></param>
        /// <param name="version"></param>
        /// <param name="date"></param>
        /// <param name="uidOperateur"></param>
        /// <param name="conf"></param>
        /// <returns>string : contenu du fichier out d'impression</returns>
        void ProcessTemplateLot(OrdreFabrication ordreFabrication, VersionArticle version, UsageCorrespondanceProduit usageCorrespondanceProduit, string date, string uidOperateur, Microsoft.Extensions.Configuration.IConfiguration _configuration);
        void ProcessTemplateMontage(OrdreFabrication ordreFabrication, UsageCorrespondanceProduit usage, string date, string date1, IConfiguration configuration);
        */
        /// <summary>
        /// Remplir le templates pour la réimpression
        /// </summary>
        /// <param name="reprintLabelLineModel"></param>
        /// <param name="version"></param>
        /// <param name="article"></param>
        /// <param name="quantite"></param>
        /// <param name="conf"></param>
        /// <returns>string : contenu du fichier out d'impression</returns>
        void ProcessTemplateReprint(Label etiquette, ManufacturingOrder ordre, LabelTemplate? usage, FirmwareVersion? version, Article article, IConfiguration configuration);
        void ProcessTemplateCommon(ManufacturingOrder ordreFabrication, FirmwareVersion version, LabelTemplate usageCorrespondanceProduit, DateTimeOffset date, string uidOperateur, Article article, IConfiguration configuration);
        void ProcessTemplatePartialPrint(ManufacturingOrder ordreFabrication, FirmwareVersion versionArticle, LabelTemplate usageCorrespondanceProduit, DateTimeOffset date, string userUid, Article article, int printQuantity, IConfiguration configuration);
        void ProcessTemplateArtBorne(Article article, string quantity, IConfiguration configuration);
        void ProcessTemplateInitials(string initials, string quantity, IConfiguration configuration);
    }
}
