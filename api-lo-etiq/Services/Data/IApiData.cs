using api_lo_etiq.Models;

namespace api_lo_etiq.Services.api_data
{
    public interface IApiData
    {
        Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(string manufacturingOrderUid);
        /// <summary>
        /// Renvoie les données relatives d'un OF
        /// </summary>
        /// <param name="uidOF">Uid de l'OF</param>
        /// <returns>OrdreFabrication</returns>
        Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(string uidOF);

        /// <summary>
        /// Renvoie les données d'une VersionArticle 
        /// </summary>
        /// <param name="uidArticle">Uid Article</param>
        /// <returns>VersionArticle</returns>
        Task<ArticleVersion?> GetVersionWithUidArticle(string uidArticle);


        /// <summary>
        /// Créée une étiquette dans la base.
        /// </summary>
        /// <param name="request">EtiquetteDto</param>
        /// <returns>Body de la requête</returns>
        Task<string> CreateEtiquette(LabelDto request);


        /// <summary>
        /// Renvoie si une etiquette existe dans la base
        /// </summary>
        /// <param name="uidAuthenti">UidProduit+CurrentSN d'une étiquette</param>
        /// <returns>Bool</returns>
        Task<bool> EtiquetteAlreadyExists(string uidAuthenti);
        /// <summary>
        /// Renvoie les opérateurs atuels dans la base
        /// </summary>
        /// <returns>Liste Opérateurs</returns>
        Task<List<User>?> GetCurrentOperateurs();
        /// <summary>
        /// Retourne la correspondance du template d'impression pour un article et un usage donnés
        /// </summary>
        /// <param name="uidProduit"></param>
        /// <param name="uidUsage"></param>
        /// <returns>UsageCorrespondanceProduit</returns>
        Task<Models.Template?> GetUsageCorrespondanceProduitWithUidProduitUidUsage(string uidProduit, string uidUsage);

        Task<Label?> GetEtiquetteByUidAuthenti(string uidAuthenti);
        Task<Article> GetArticleByUidArticle(string uidProduit);
        Task<Label> GetLabelLastSerialNumberPrinted(string manufacturingOrderUid);
        Task<User?> GetUserByUserUid(string uidOperateur);
    }
}
