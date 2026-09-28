using System.Net;
using System.Text;
using Api.PrinterProd.Models;
using Api.PrinterProd.Services.api_data;
using Api.PrinterProd.Services.Labels;
using DataLib;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.PrinterProd.IntegrationTests;

public sealed class LabelBatchingTests
{
    [Fact]
    public async Task ApiData_SendsTheWholeBatchInOneRequest()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var configuration = CreateConfiguration();
        var apiData = new ApiData(httpClient, configuration);
        var labels = new[] { CreateLabel("AUTH-1", 100001), CreateLabel("AUTH-2", 100002) };

        await apiData.CreateEtiquettes(labels);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal("api/Labels/create-batch", handler.RequestUri);
        Assert.Contains("AUTH-1", handler.RequestBody);
        Assert.Contains("AUTH-2", handler.RequestBody);
    }

    [Fact]
    public async Task LabelsCreator_BuildsA1500LabelBatchWithoutIndividualExistenceCalls()
    {
        var apiData = new CapturingApiData();
        var creator = new LabelsCreator(apiData, CreateConfiguration());
        var manufacturingOrder = new ManufacturingOrder
        {
            ManufacturingOrderUid = 12345,
            ProductUid = "71PT00640A",
            Quantity = 1500,
            FirstSerialNumber = 100001,
            InternalOrderNumber = "INTERNAL",
            ExternalOrderNumber = "EXTERNAL",
            Status = "ACTIVE"
        };
        var template = new LabelTemplate
        {
            LabelTemplateUid = "TEMPLATE-1",
            Product = manufacturingOrder.ProductUid,
            LabelTypeUid = "LABEL LOT",
            TemplateFile = "lot.prn",
            Photo = "lot.png",
            StartDate = DateTimeOffset.MinValue,
            EndDate = DateTimeOffset.MaxValue,
            UserUid = "USER-1"
        };
        var version = new FirmwareVersion
        {
            FirmwareVersionUid = "VERSION-1",
            ArticleUid = manufacturingOrder.ProductUid,
            Version = "1.0"
        };

        await Assert.ThrowsAsync<BatchCapturedException>(() => creator.ProcessAndCreateLabelsCommon(
            DateTimeOffset.Parse("2026-09-22T08:00:00+02:00"),
            "USER-1",
            manufacturingOrder,
            template,
            version,
            "unused.prn"));

        Assert.Equal(1, apiData.BatchCallCount);
        Assert.Equal(1500, apiData.CapturedLabels.Count);
        Assert.Equal(0, apiData.ExistenceCallCount);
        Assert.Equal(1500, apiData.CapturedLabels.Select(label => label.AuthentiUid).Distinct().Count());
        Assert.Equal(100001, apiData.CapturedLabels.First().CurrentSerialNumber);
        Assert.Equal(101500, apiData.CapturedLabels.Last().CurrentSerialNumber);
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:Uri"] = "http://localhost/",
            ["Api:Endpoints:CreateLabels"] = "api/Labels/create-batch",
            ["Printing:AuthentiUidProductPrefixLength"] = "9"
        })
        .Build();

    private static LabelDto CreateLabel(string authentiUid, int serialNumber) => new()
    {
        AuthentiUid = authentiUid,
        UserUid = "USER-1",
        ProductUid = "PRODUCT-1",
        ManufacturingOrderUid = 12345,
        CreationDate = DateTimeOffset.Parse("2026-09-22T08:00:00+02:00"),
        CurrentSerialNumber = serialNumber,
        ProductVersion = "1.0",
        LabelTypeUid = "LABEL LOT",
        Template = "lot.prn"
    };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string? RequestUri { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUri = request.RequestUri?.PathAndQuery.TrimStart('/');
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"createdCount\":2}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class CapturingApiData : IApiData
    {
        public int BatchCallCount { get; private set; }
        public int ExistenceCallCount { get; private set; }
        public IReadOnlyCollection<LabelDto> CapturedLabels { get; private set; } = [];

        public Task<List<Label>> GetCollectionLabelsByManufacturingOrderUid(int manufacturingOrderUid) => Task.FromResult(new List<Label>());

        public Task CreateEtiquettes(IReadOnlyCollection<LabelDto> requests)
        {
            BatchCallCount++;
            CapturedLabels = requests;
            throw new BatchCapturedException();
        }

        public Task<bool> EtiquetteAlreadyExists(string uidAuthenti)
        {
            ExistenceCallCount++;
            return Task.FromResult(false);
        }

        public Task<string> CreateEtiquette(LabelDto request) => throw new NotSupportedException();
        public Task<ManufacturingOrder?> GetOrdreFabricationWithUidOF(int uidOF) => throw new NotSupportedException();
        public Task<FirmwareVersion?> GetVersionWithUidArticle(string uidArticle) => throw new NotSupportedException();
        public Task<LabelTemplate?> GetUsageCorrespondanceProduitWithUidProduitUidUsage(string uidProduit, string uidUsage) => throw new NotSupportedException();
        public Task<Label?> GetEtiquetteByUidAuthenti(string uidAuthenti) => throw new NotSupportedException();
        public Task<Article> GetArticleByUidArticle(string uidProduit) => throw new NotSupportedException();
        public Task<Label?> GetLabelLastSerialNumberPrinted(int manufacturingOrderUid) => throw new NotSupportedException();
        public Task<User> GetUserByUid(string userUid) => throw new NotSupportedException();
        public Task<KitTraceability> GetKitTraceaByProductAndSerialNumber(string productUid, int serialNumber) => throw new NotSupportedException();
    }

    private sealed class BatchCapturedException : Exception
    {
    }
}
