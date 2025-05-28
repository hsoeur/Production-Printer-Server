using api_lo_etiq.Models;

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
        void ProcessTemplateReprint(Label etiquette, ManufacturingOrder ordre, Models.Template? usage, ArticleVersion? version, Article article, IConfiguration configuration);
        void ProcessTemplateCommon(ManufacturingOrder ordreFabrication, ArticleVersion version, Models.Template usageCorrespondanceProduit, string date, string uidOperateur, Article article, Microsoft.Extensions.Configuration.IConfiguration configuration);
        void ProcessTemplatePartialPrint(ManufacturingOrder ordreFabrication, ArticleVersion versionArticle, Models.Template usageCorrespondanceProduit, string date, string userUid, Article article, string printQuantity, IConfiguration configuration);
        void ProcessTemplateArtBorne(Article article, string quantity, IConfiguration configuration);
        void ProcessTemplateInitials(string initials, string quantity, IConfiguration configuration);
    }
}
