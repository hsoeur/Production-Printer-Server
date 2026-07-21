using Api.PrinterProd.Configuration;
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
            _httpClient.BaseAddress = new Uri(_configuration.GetRequiredString("Api:Uri"));
        }
        private string GetEndpoint(string endpointName)
        {
            return _configuration.GetRequiredString($"Api:Endpoints:{endpointName}");
        }

        private string FormatEndpoint(string endpointName, params (string Name, string Value)[] values)
        {
            string endpoint = GetEndpoint(endpointName);
            foreach (var value in values)
            {
                endpoint = endpoint.Replace($"{{{value.Name}}}", Uri.EscapeDataString(value.Value));
            }

            return endpoint;
        }
        public async Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(int uidOF)
        {
            // Updated endpoint to new route provided
            var uri = FormatEndpoint("ManufacturingOrderByUid", ("manufacturingOrderUid", uidOF.ToString()));

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ManufacturingOrder>(content);

        }


        public async Task<FirmwareVersion?> GetVersionWithUidArticle(string uidArticle)
        {
            var uri = FormatEndpoint("FirmwareVersionByArticleUid", ("articleUid", uidArticle));

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
            string uri = GetEndpoint("CreateLabel");

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
            var uri = FormatEndpoint("LabelExistsByAuthentiUid", ("authentiUid", uidAuthenti));

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
            var uri = FormatEndpoint("LabelTemplateByArticleAndUsage", ("uidProduit", uidProduit), ("usage", uidUsage));

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
            var uri = FormatEndpoint("LabelByAuthentiUid", ("authentiUid", uidAuthenti));

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Label>(content);
        }

        public async Task<Article> GetArticleByUidArticle(string uidProduit)
        {
            var uri = FormatEndpoint("ArticleByArticleUid", ("articleUid", uidProduit));

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Article>(content);
        }

        public async Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(int manufacturingOrderUid)
        {
            var uri = FormatEndpoint("LabelsByManufacturingOrder", ("manufacturingOrderUid", manufacturingOrderUid.ToString()));

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<Label>>(content);
        }

        public async Task<Label> GetLabelLastSerialNumberPrinted(int manufacturingOrderUid)
        {
            var uri = FormatEndpoint("LastSerialNumberByManufacturingOrder", ("manufacturingOrderUid", manufacturingOrderUid.ToString()));

            var response = await _httpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Label>(content);
        }

        public async Task<User> GetUserByUid(string userUid)
        {
            try
            {
                var url = FormatEndpoint("UserByUserUid", ("userUid", userUid));
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

        public async Task<KitTraceability> GetKitTraceaByProductAndSerialNumber(string productUid, int serialNumber)
        {
            try
            {
                var uri = FormatEndpoint("KitTraceabilityByProductSerial", ("productUid", productUid), ("serialNumber", serialNumber.ToString()));

                var response = await _httpClient.GetAsync(uri);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Log.Information("Aucune traçabilité kit trouvée pour le produit : {productUid} et le numéro de série : {SerialNumber}", productUid, serialNumber);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<KitTraceability>(content);
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error(httpEx, "Erreur HTTP lors de l'appel à GetKitTraceaByUidOFAndSerialNumber avec l'OF : {ManufacturingOrderUid} et le numéro de série : {SerialNumber}", productUid, serialNumber);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de la récupération de la traçabilité kit pour l'OF : {ManufacturingOrderUid} et le numéro de série : {SerialNumber}", productUid, serialNumber);
                return null;
            }
        }
    }
}
