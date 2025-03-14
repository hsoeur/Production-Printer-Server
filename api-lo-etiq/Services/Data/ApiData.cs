using api_lo_etiq.Models;
using Newtonsoft.Json;
using System.Text;

namespace api_lo_etiq.Services.api_data
{
    public class ApiData : IApiData
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ApiData(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration["Api:Uri"]);
        }

        public async Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(string uidOF)
        {
            var uri = $"api/ManufacturingOrder/GetManufacturingOrderByManufacturingOrderUid?manufacturingOrderUid={uidOF}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ManufacturingOrder>(content);

        }

       
        public async Task<ArticleVersion?> GetVersionWithUidArticle(string uidArticle)
        {
            var uri = $"api/ArticleVersion/GetArticleVersionByArticleUid?articleUid={uidArticle}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ArticleVersion>(content);

        }
        /// <summary>
        /// Méthode pour appeler le endpoint qui permet la création/enregistrement d'une étiquette dans la base de données de production.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<string> CreateEtiquette(LabelDto request)
        {
            string uri = "api/Label/CreateLabel";

            var jsonUserData = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonUserData, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    return jsonResponse;
                }
                else
                {
                    return "Erreur : " + response.StatusCode;
                }
            }
            catch (Exception e)
            {
                return "Erreur lors de la demande : " + e.Message;
            }
        }
       
        public async Task<bool> EtiquetteAlreadyExists(string uidAuthenti)
        {
            var uri = $"api/Label/LabelExists?authentiUid={uidAuthenti}";

            var response = await _httpClient.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public async Task<List<User>?> GetCurrentOperateurs()
        {
            var uri = "api/Operator/GetCollectionCurrentOperator";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<User>>(content);
        }
        /// <summary>
        /// Récupération des données de correspondance entre un produit un template et un usage.
        /// </summary>
        /// <param name="uidProduit"></param>
        /// <param name="uidUsage"></param>
        /// <returns></returns>
        public async Task<Models.Template?> GetUsageCorrespondanceProduitWithUidProduitUidUsage(string uidProduit, string uidUsage)
        {
            var uri = $"api/Template/GetTemplateByArticleUidAndUsageUid?uidProduit={uidProduit}&usage={uidUsage}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Models.Template>(content);
        }
        /// <summary>
        /// Récupérer les données de l'étiquette sélectionnée pour lé réimpression.
        /// </summary>
        /// <param name="uidAuthenti"></param>
        /// <returns></returns>
        public async Task<Label?> GetEtiquetteByUidAuthenti(string uidAuthenti)
        {
            var uri = $"api/Label/GetLabelByAuthentiUid?authentiUid={uidAuthenti}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Label>(content);
        }

        public async Task<Article> GetArticleByUidArticle(string uidProduit)
        {
            var uri = $"api/Article/GetArticleByArticleUid?articleUid={uidProduit}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Article>(content); 
        }

        public async Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(string manufacturingOrderUid)
        {
            var uri = $"api/Label/GetCollectionLabelByManufacturingOrderUid?manufacturingOrderUid={manufacturingOrderUid}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<Label>>(content);
        }

        public async Task<Label> GetLabelLastSerialNumberPrinted(string manufacturingOrderUid)
        {
            var uri = $"api/Label/GetLabelLastSerialNumberPrintedByManufacturingOrderUid?manufacturingOrderUid={manufacturingOrderUid}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Label>(content);
        }
    }
}
