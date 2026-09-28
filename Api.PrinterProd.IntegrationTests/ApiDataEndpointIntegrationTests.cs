using System.Text.Json;
using Api.PrinterProd.Services.api_data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.PrinterProd.IntegrationTests;

public sealed class ApiDataEndpointIntegrationTests
{
    private readonly TestSettings _settings;
    private readonly ApiData _apiData;
    private readonly HttpClient _httpClient;

    public ApiDataEndpointIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.IntegrationTests.json", optional: false)
            .AddEnvironmentVariables(prefix: "PRINTERPROD_TEST_")
            .Build();

        _settings = new TestSettings
        {
            BaseUri = configuration["Api:Uri"]!,
            ManufacturingOrderUid = configuration.GetValue<int>("TestData:ManufacturingOrderUid"),
            ProductUid = configuration["TestData:ProductUid"]!,
            FirmwareVersion = configuration["TestData:FirmwareVersion"]!,
            UserUid = configuration["TestData:UserUid"]!,
            TemplateProductUid = configuration["TestData:TemplateProductUid"]!,
            TemplateUsage = configuration["TestData:TemplateUsage"]!,
            MissingAuthentiUid = configuration["TestData:MissingAuthentiUid"]!,
            MissingKitProduct = configuration["TestData:MissingKitProduct"]!,
            MissingKitSerialNumber = configuration.GetValue<int>("TestData:MissingKitSerialNumber")
        };

        _httpClient = new HttpClient { BaseAddress = new Uri(_settings.BaseUri), Timeout = TimeSpan.FromSeconds(15) };
        _apiData = new ApiData(_httpClient, configuration);
    }

    [Fact]
    public async Task Local_api_data_is_connected_to_the_test_database()
    {
        using var response = await _httpClient.GetAsync("api/Healths/Database");
        var json = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.True(JsonDocument.Parse(json).RootElement.GetProperty("database").GetBoolean());
    }

    [Fact]
    public async Task Manufacturing_order_is_loaded_from_the_test_database()
    {
        var order = await _apiData.GetOrdreFabricationWithUidOF(_settings.ManufacturingOrderUid);

        Assert.NotNull(order);
        Assert.Equal(_settings.ManufacturingOrderUid, order.ManufacturingOrderUid);
        Assert.Equal(_settings.ProductUid, order.ProductUid);
    }

    [Fact]
    public async Task Article_and_firmware_are_loaded_from_the_test_database()
    {
        var article = await _apiData.GetArticleByUidArticle(_settings.ProductUid);
        var firmware = await _apiData.GetVersionWithUidArticle(_settings.ProductUid);

        Assert.Equal(_settings.ProductUid, article.ArticleUid);
        Assert.NotNull(firmware);
        Assert.Equal(_settings.ProductUid, firmware.ArticleUid);
        Assert.Equal(_settings.FirmwareVersion, firmware.Version);
    }

    [Fact]
    public async Task User_is_loaded_from_the_test_database()
    {
        var user = await _apiData.GetUserByUid(_settings.UserUid);

        Assert.NotNull(user);
        Assert.Equal(_settings.UserUid, user.UserUid);
    }

    [Fact]
    public async Task Label_template_is_loaded_from_the_test_database()
    {
        var template = await _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(
            _settings.TemplateProductUid,
            _settings.TemplateUsage);

        Assert.NotNull(template);
        Assert.Equal(_settings.TemplateProductUid, template.Product);
        Assert.Equal(_settings.TemplateUsage, template.LabelTypeUid);
    }

    [Fact]
    public async Task Missing_label_state_is_read_from_the_test_database()
    {
        Assert.False(await _apiData.EtiquetteAlreadyExists(_settings.MissingAuthentiUid));
        Assert.Null(await _apiData.GetEtiquetteByUidAuthenti(_settings.MissingAuthentiUid));
        Assert.Empty(await _apiData.GetCollectionLabelsByManufacturingOrderUid(_settings.ManufacturingOrderUid));
        Assert.Null(await _apiData.GetLabelLastSerialNumberPrinted(_settings.ManufacturingOrderUid));
    }

    [Fact]
    public async Task Missing_kit_state_is_read_from_the_test_database()
    {
        var kit = await _apiData.GetKitTraceaByProductAndSerialNumber(
            _settings.MissingKitProduct,
            _settings.MissingKitSerialNumber);

        Assert.Null(kit);
    }

    private sealed class TestSettings
    {
        public required string BaseUri { get; init; }
        public required int ManufacturingOrderUid { get; init; }
        public required string ProductUid { get; init; }
        public required string FirmwareVersion { get; init; }
        public required string UserUid { get; init; }
        public required string TemplateProductUid { get; init; }
        public required string TemplateUsage { get; init; }
        public required string MissingAuthentiUid { get; init; }
        public required string MissingKitProduct { get; init; }
        public required int MissingKitSerialNumber { get; init; }
    }
}
