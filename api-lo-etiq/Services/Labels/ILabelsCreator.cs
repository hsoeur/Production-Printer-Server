using api_lo_etiq.Models;

namespace api_lo_etiq.Services.Labels
{
    public interface ILabelsCreator
    {
        /*
        int ProcessAndCreateLabelsAccessoire(string date, string uidOperateur, OrdreFabrication ordreFabrication, UsageCorrespondanceProduit usageCorrespondanceProduit);
        int ProcessAndCreateLabelsEmballage(string date, string uidOperateur, OrdreFabrication ordreFabrication, UsageCorrespondanceProduit usage);
        int ProcessAndCreateLabelsLot(string date, string uidOperateur, OrdreFabrication? ordreFabrication, UsageCorrespondanceProduit? usageTemplate, VersionArticle? version);
        */
        Task<int> ProcessAndCreateLabelsCommon(string date, string uidOperateur, ManufacturingOrder? ordreFabrication, Models.Template? usageTemplate, ArticleVersion? version);
        int ProcessAndCreateLabelsPartialPrint(string date, string userUid, ManufacturingOrder ordreFabrication, Models.Template usageCorrespondanceProduit, ArticleVersion versionArticle, string printQuantity);
        //int ProcessAndCreateLabelsMontage(string date, string uidOperateur, OrdreFabrication ordreFabrication, UsageCorrespondanceProduit usage);
    }
}
