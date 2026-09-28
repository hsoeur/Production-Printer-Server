using DataLib;
using Api.PrinterProd.Models;

namespace Api.PrinterProd.Services.api_data
{
    public interface IApiData
    {
        Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(int manufacturingOrderUid);
        Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(int uidOF);
        Task<FirmwareVersion?> GetVersionWithUidArticle(string uidArticle);
        Task<string> CreateEtiquette(LabelDto request);
        Task CreateEtiquettes(IReadOnlyCollection<LabelDto> requests);
        Task<bool> EtiquetteAlreadyExists(string uidAuthenti);
        Task<LabelTemplate?> GetUsageCorrespondanceProduitWithUidProduitUidUsage(string uidProduit, string uidUsage);
        Task<Label?> GetEtiquetteByUidAuthenti(string uidAuthenti);
        Task<Article> GetArticleByUidArticle(string uidProduit);
        Task<Label?> GetLabelLastSerialNumberPrinted(int manufacturingOrderUid);
        Task<User> GetUserByUid(string userUid);
        Task<KitTraceability> GetKitTraceaByProductAndSerialNumber(string productUid, int serialNumber);
    }
}
