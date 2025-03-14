using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using api_lo_etiq.Controllers;
using api_lo_etiq.Services.api_data;
using api_lo_etiq.Services.Template;
using api_lo_etiq.Services.Labels;
using api_lo_etiq.Models;
using api_lo_etiq.Services.Printer;

namespace ApiEtiq.Tests
{
    public class PrintCommonTests
    {
        private readonly Mock<IApiData> _mockApiData;
        private readonly Mock<ILabelTemplateProcessor> _mockLabelTemplateProcessor;
        private readonly Mock<ILabelsCreator> _mockLabelsCreator;
        private readonly Mock<IPrinter> _mockPrinter;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly PrintController _controller;

        public PrintCommonTests()
        {
            _mockApiData = new Mock<IApiData>();
            _mockLabelTemplateProcessor = new Mock<ILabelTemplateProcessor>();
            _mockLabelsCreator = new Mock<ILabelsCreator>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockPrinter = new Mock<IPrinter>();

            _controller = new PrintController(
                _mockApiData.Object,
                _mockLabelTemplateProcessor.Object,
                _mockConfiguration.Object,
                _mockLabelsCreator.Object,
                _mockPrinter.Object
            );
        }

        [Fact]
        public async Task PrintCommon_ShouldReturn_SUCCESS_When_AllChecksPass()
        {
            _mockApiData.Setup(x => x.GetOrdreFabricationWithUidOF(It.IsAny<string>()))
                .ReturnsAsync(new ManufacturingOrder { FirstSerialNumber = "100001", ProductUid = "61PTOO512C" });

            _mockApiData.Setup(x => x.GetUsageCorrespondanceProduitWithUidProduitUidUsage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Template());

            _mockApiData.Setup(x => x.GetVersionWithUidArticle(It.IsAny<string>()))
                .ReturnsAsync(new ArticleVersion());

            _mockApiData.Setup(x => x.GetArticleByUidArticle(It.IsAny<string>()))
                .ReturnsAsync(new Article());

            _mockLabelsCreator.Setup(x => x.ProcessAndCreateLabelsCommon(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ManufacturingOrder>(),
                It.IsAny<Template>(),
                It.IsAny<ArticleVersion>()))
                .ReturnsAsync(1);

            var result = await _controller.PrintCommon("", "", "");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("SUCCESS", okResult.Value);
        }

        [Fact]
        public async Task PrintCommon_ShouldReturn_ERR_OF_NOT_FOUND_IN_DB_When_ManufacturingOrder_Is_Null()
        {
            _mockApiData.Setup(x => x.GetOrdreFabricationWithUidOF(It.IsAny<string>()))
                .ReturnsAsync((ManufacturingOrder)null);

            var result = await _controller.PrintCommon("71", "24873", "Etiquette Lot");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ERR_OF_NOT_FOUND_IN_DB", okResult.Value);
        }

        [Fact]
        public async Task PrintCommon_ShouldReturn_ERR_OF_SERIAL_NUMBER_When_FirstSerialNumber_Is_Less_Than_100001()
        {
            _mockApiData.Setup(x => x.GetOrdreFabricationWithUidOF(It.IsAny<string>()))
                .ReturnsAsync(new ManufacturingOrder { FirstSerialNumber = "99999" });

            var result = await _controller.PrintCommon("71", "24873", "Etiquette Lot");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ERR_OF_SERIAL_NUMBER", okResult.Value);
        }

        [Fact]
        public async Task PrintCommon_ShouldReturn_ERR_USAGE_NOT_FOUND_IN_DB_When_Template_Is_Null()
        {
            _mockApiData.Setup(x => x.GetOrdreFabricationWithUidOF(It.IsAny<string>()))
                .ReturnsAsync(new ManufacturingOrder { FirstSerialNumber = "100001", ProductUid = "61PTOO512C" });

            _mockApiData.Setup(x => x.GetUsageCorrespondanceProduitWithUidProduitUidUsage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Template)null);

            var result = await _controller.PrintCommon("71", "24873", "Etiquette Lot");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ERR_USAGE_NOT_FOUND_IN_DB", okResult.Value);
        }

        [Fact]
        public async Task PrintCommon_ShouldReturn_ERR_VERSION_NOT_FOUND_When_ArticleVersion_Is_Null()
        {
            _mockApiData.Setup(x => x.GetOrdreFabricationWithUidOF(It.IsAny<string>()))
                .ReturnsAsync(new ManufacturingOrder { FirstSerialNumber = "100001", ProductUid = "61PTOO512C" });

            _mockApiData.Setup(x => x.GetUsageCorrespondanceProduitWithUidProduitUidUsage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Template());

            _mockApiData.Setup(x => x.GetVersionWithUidArticle(It.IsAny<string>()))
                .ReturnsAsync((ArticleVersion)null);

            var result = await _controller.PrintCommon("71", "24873", "Etiquette Lot");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ERR_VERSION_NOT_FOUND", okResult.Value);
        }

        [Fact]
        public async Task PrintCommon_ShouldReturn_ERR_ARTICLE_NOT_FOUND_When_Article_Is_Null()
        {
            _mockApiData.Setup(x => x.GetOrdreFabricationWithUidOF(It.IsAny<string>()))
                .ReturnsAsync(new ManufacturingOrder { FirstSerialNumber = "100001", ProductUid = "61PTOO512C" });

            _mockApiData.Setup(x => x.GetUsageCorrespondanceProduitWithUidProduitUidUsage(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Template());

            _mockApiData.Setup(x => x.GetVersionWithUidArticle(It.IsAny<string>()))
                .ReturnsAsync(new ArticleVersion());

            _mockApiData.Setup(x => x.GetArticleByUidArticle(It.IsAny<string>()))
                .ReturnsAsync((Article)null);

            var result = await _controller.PrintCommon("71", "24873", "Etiquette Lot");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ERR_ARTICLE_NOT_FOUND", okResult.Value);
        }
    }
}
