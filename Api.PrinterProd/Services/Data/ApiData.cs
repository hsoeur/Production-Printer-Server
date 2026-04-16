using Api.PrinterProd.Models;
using DataLib;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Text.Json;

namespace Api.PrinterProd.Services.api_data
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

        public async Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(int uidOF)
        {
            // Updated endpoint to new route provided
            var uri = $"api/ManufacturingOrder/by-manufacturing-order-uid?manufacturingOrderUid={uidOF}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ManufacturingOrder>(content);

        }


        public async Task<FirmwareVersion?> GetVersionWithUidArticle(string uidArticle)
        {
            var uri = $"api/FirmwareVersions/by-article-uid?articleUid={uidArticle}";

            var response = await _httpClient.GetAsync(uri);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Information("Aucune version firmware trouvée pour l'article : {UidArticle}", uidArticle);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<FirmwareVersion>(content);

        }
        /// <summary>
        /// Méthode pour appeler le endpoint qui permet la création/enregistrement d'une étiquette dans la base de données de production.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<string> CreateEtiquette(LabelDto request)
        {
            // Updated endpoint to new route
            string uri = "api/Labels/create";

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
            var uri = $"api/Labels/exists-by-authenti-uid?authentiUid={uidAuthenti}";

            var response = await _httpClient.GetAsync(uri);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Récupération des données de correspondance entre un produit un template et un usage.
        /// </summary>
        /// <param name="uidProduit"></param>
        /// <param name="uidUsage"></param>
        /// <returns></returns>
        public async Task<LabelTemplate?> GetUsageCorrespondanceProduitWithUidProduitUidUsage(string uidProduit, string uidUsage)
        {
            var uri = $"api/LabelTemplate/by-article-uid-and-usage?uidProduit={uidProduit}&usage={uidUsage}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<LabelTemplate>(content);
        }
        /// <summary>
        /// Récupérer les données de l'étiquette sélectionnée pour lé réimpression.
        /// </summary>
        /// <param name="uidAuthenti"></param>
        /// <returns></returns>
        public async Task<Label?> GetEtiquetteByUidAuthenti(string uidAuthenti)
        {
            var uri = $"api/Labels/by-authenti-uid?authentiUid={uidAuthenti}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Label>(content);
        }

        public async Task<Article> GetArticleByUidArticle(string uidProduit)
        {
            var uri = $"api/Articles/by-article-uid?articleUid={uidProduit}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Article>(content);
        }

        public async Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(int manufacturingOrderUid)
        {
            var uri = $"api/Labels/by-manufacturing-order?manufacturingOrderUid={manufacturingOrderUid}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<Label>>(content);
        }

        public async Task<Label> GetLabelLastSerialNumberPrinted(int manufacturingOrderUid)
        {
            var uri = $"api/Labels/last-serial-number-by-manufacturing-order?manufacturingOrderUid={manufacturingOrderUid}";

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Label>(content);
        }

        public async Task<User> GetUserByUid(string userUid)
        {
            try
            {
                var url = $"api/Users/by-user-uid?userUid={userUid}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var user = System.Text.Json.JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return user!;
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error(httpEx, "Erreur HTTP lors de l'appel à GetUserByUid avec l'UID : {UserUid}", userUid);
                return null;
            }
        }
    }
}
