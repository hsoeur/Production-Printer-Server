using DataLib;
using api_lo_etiq.Models;

namespace api_lo_etiq.Services.api_data
{
    public interface IApiData
    {
        Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(int manufacturingOrderUid);
        Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(int uidOF);
        Task<FirmwareVersion?> GetVersionWithUidArticle(string uidArticle);
        Task<string> CreateEtiquette(LabelDto request);
        Task<bool> EtiquetteAlreadyExists(string uidAuthenti);
        Task<LabelTemplate?> GetUsageCorrespondanceProduitWithUidProduitUidUsage(string uidProduit, string uidUsage);
        Task<Label?> GetEtiquetteByUidAuthenti(string uidAuthenti);
        Task<Article> GetArticleByUidArticle(string uidProduit);
        Task<Label> GetLabelLastSerialNumberPrinted(int manufacturingOrderUid);
        Task<User> GetUserByUid(string userUid);
    }
}
