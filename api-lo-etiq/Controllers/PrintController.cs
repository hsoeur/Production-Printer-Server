using api_lo_etiq.Models;
using api_lo_etiq.Services.api_data;
using api_lo_etiq.Services.Labels;
using api_lo_etiq.Services.Printer;
using api_lo_etiq.Services.Template;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace api_lo_etiq.Controllers
{
    /// <summary>
    /// CLasse PrintController qui expose des endpoints pour l'impression et la réimpression des étiquettes de production.
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
   
    public class PrintController : Controller
    {
        private readonly IApiData _apiData;
        private readonly ILabelTemplateProcessor _labelTemplateProcessor;
        private readonly IConfiguration _configuration;
        private readonly ILabelsCreator _labelsCreator;
        private readonly IPrinter _printer;
        
        /// <summary>
        /// Constructeur de la classe PrintController
        /// </summary>
        /// <param name="apiData"></param>
        /// <param name="labelTemplateProcessor"></param>
        /// <param name="configuration"></param>
        /// <param name="labelsCreator"></param>
        /// <param name="printer"></param>
        public PrintController(
            IApiData apiData,
            ILabelTemplateProcessor labelTemplateProcessor,
            IConfiguration configuration,
            ILabelsCreator labelsCreator,
            IPrinter printer)
        {
            _apiData = apiData;
            _labelTemplateProcessor = labelTemplateProcessor;
            _configuration = configuration;
            _labelsCreator = labelsCreator;
            _printer = printer;
        }


        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ConnectionState()
        {
            try
            {
                var of = await _apiData.GetOrdreFabricationWithUidOF("25247");

                return Ok("Connexion Successful.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return BadRequest(ex.Message);
            }

        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PrintFile()
        {
            try
            {
                _printer.PrintFile();
                Log.Information("Impression Terminée avec Succès");
                return Ok("Impression Successful.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Méthode PrintCommon responsable de l'impresssion normal des étiquettes de production.
        /// </summary>
        /// <param name="uidOperateur"></param>
        /// <param name="uidOrdreFabrication"></param>
        /// <param name="uidUsage"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200)]
       
        public async Task<IActionResult> PrintCommon(string uidOperateur, string uidOrdreFabrication, string uidUsage)
        {
            Log.Information("PrintCommon method called with parameters: uidOperateur={uidOperateur}, uidOrdreFabrication={uidOrdreFabrication}, uidUsage={uidUsage}", uidOperateur, uidOrdreFabrication, uidUsage);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            ManufacturingOrder ordreFabrication = _apiData.GetOrdreFabricationWithUidOF(uidOrdreFabrication).Result;

            if (ordreFabrication == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", uidOrdreFabrication);
                return Ok("ERR_OF_NOT_FOUND_IN_DB");
            }

            if(int.Parse(ordreFabrication.FirstSerialNumber) < 100001 && int.Parse(ordreFabrication.FirstSerialNumber) != 0)
            {
                Log.Warning("Serial number of OF {uidOrdreFabrication} is less than 100001", uidOrdreFabrication);
                return Ok("ERR_OF_SERIAL_NUMBER");
            }

            Template usageCorrespondanceProduit = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.ProductUid, uidUsage).Result;

            if (usageCorrespondanceProduit == null)
            {
                Log.Warning("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordreFabrication.ProductUid, uidUsage);
                return Ok("ERR_USAGE_NOT_FOUND_IN_DB");
            }

            ArticleVersion versionArticle = _apiData.GetVersionWithUidArticle(ordreFabrication.ProductUid).Result;

            if (versionArticle == null)
            {
                Log.Warning("VersionArticle not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                return Ok("ERR_VERSION_NOT_FOUND");
            }

            Article article = _apiData.GetArticleByUidArticle(ordreFabrication.ProductUid).Result;

            if (article == null)
            {
                Log.Warning("Article not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                return Ok("ERR_ARTICLE_NOT_FOUND");
            }

            _labelTemplateProcessor.ProcessTemplateCommon(ordreFabrication, versionArticle, usageCorrespondanceProduit, date, uidOperateur, article, _configuration);
            int res = await _labelsCreator.ProcessAndCreateLabelsCommon(date, uidOperateur, ordreFabrication, usageCorrespondanceProduit, versionArticle);

            if (res == -1)
            {
                Log.Warning("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
                return Ok("ERR_ETI_ALREADY_IN_DB");
            }
            return Ok("SUCCESS");
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult PartialPrint(string userUid, string manufacturingOrderuid, string printQuantity)
        {
            try
            {
                string date = DateTime.Now.ToString("dd/MM/yyyy");
                string usage = "Etiquette Lot";

                ManufacturingOrder ordreFabrication = _apiData.GetOrdreFabricationWithUidOF(manufacturingOrderuid).Result;

                if (ordreFabrication == null)
                {
                    Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", manufacturingOrderuid);
                    return Ok("ERR_OF_NOT_FOUND_IN_DB");
                }

                if (int.Parse(ordreFabrication.FirstSerialNumber) < 100001 && int.Parse(ordreFabrication.FirstSerialNumber) != 0)
                {
                    Log.Warning("Serial number of OF {uidOrdreFabrication} is less than 100001", manufacturingOrderuid);
                    return Ok("ERR_OF_SERIAL_NUMBER");
                }

                Template usageCorrespondanceProduit = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.ProductUid, usage).Result;

                if (usageCorrespondanceProduit == null)
                {
                    Log.Warning("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordreFabrication.ProductUid, usage);
                    return Ok("ERR_USAGE_NOT_FOUND_IN_DB");
                }

                ArticleVersion versionArticle = _apiData.GetVersionWithUidArticle(ordreFabrication.ProductUid).Result;

                if (versionArticle == null)
                {
                    Log.Warning("VersionArticle not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return Ok("ERR_VERSION_NOT_FOUND");
                }

                Article article = _apiData.GetArticleByUidArticle(ordreFabrication.ProductUid).Result;

                if (article == null)
                {
                    Log.Warning("Article not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return Ok("ERR_ARTICLE_NOT_FOUND");
                }

                _labelTemplateProcessor.ProcessTemplatePartialPrint(ordreFabrication, versionArticle, usageCorrespondanceProduit, date, userUid, article, printQuantity,_configuration);
                int res = _labelsCreator.ProcessAndCreateLabelsPartialPrint(date, userUid, ordreFabrication, usageCorrespondanceProduit, versionArticle, printQuantity);

                if (res == -1)
                {
                    Log.Warning("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", manufacturingOrderuid, userUid);
                    return Ok("ERR_ETI_ALREADY_IN_DB");
                }

                return Ok("SUCCESS");

            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        /*
        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult PrintLot(string uidOperateur, string uidOrdreFabrication, string uidUsage)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Log.Information("GenerateTemplate called with uidOperateur: {uidOperateur}, uidOrdreFabrication: {uidOrdreFabrication}, uidUsage: {uidUsage}", uidOperateur, uidOrdreFabrication, uidUsage);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            OrdreFabrication ordreFabrication = _apiData.GetOrdreFabricationWithUidOF(uidOrdreFabrication).Result;

            if (ordreFabrication == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", uidOrdreFabrication);
                return Ok("ERR_OF_NOT_FOUND_IN_DB");
            }
                
            UsageCorrespondanceProduit usageCorrespondanceProduit = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.UidProduit, uidUsage).Result;

            if (usageCorrespondanceProduit == null)
            {
                Log.Warning("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordreFabrication.UidProduit, uidUsage);
                return Ok("ERR_USAGE_NOT_FOUND_IN_DB");
            }

            VersionArticle versionArticle = _apiData.GetVersionWithUidArticle(ordreFabrication.UidProduit).Result;

            if (versionArticle == null)
            {
                Log.Warning("VersionArticle not found for uidProduit: {uidProduit}", ordreFabrication.UidProduit);
                return Ok("ERR_VERSION_NOT_FOUND");
                
            }

            _labelTemplateProcessor.ProcessTemplateLot(ordreFabrication, versionArticle, usageCorrespondanceProduit, date, uidOperateur, _configuration);
            int res = _labelsCreator.ProcessAndCreateLabelsLot(date, uidOperateur, ordreFabrication, usageCorrespondanceProduit, versionArticle);

            if (res == -1)
            {
                Log.Warning("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
                return Ok("ERR_ETI_ALREADY_IN_DB");
            }

            Log.Information("Template generated successfully for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
            stopwatch.Stop();
            Log.Information("PrintLot method executed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            return Ok("SUCCESS");
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult PrintMontage(string uidOperateur,string uidOrdreFabrication, string uidUsage)
        {
            Log.Information("GenerateTemplate called with uidOperateur: {uidOrdreFabrication}",uidOrdreFabrication);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            OrdreFabrication ordreFabrication = _apiData.GetOrdreFabricationWithUidOF(uidOrdreFabrication).Result;

            if (ordreFabrication == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", uidOrdreFabrication);
                return Ok("ERR_OF_NOT_FOUND_IN_DB");
            }

            UsageCorrespondanceProduit usageCorrespondanceProduit = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.UidProduit, uidUsage).Result;

            if (usageCorrespondanceProduit == null)
            {
                Log.Warning("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordreFabrication.UidProduit, uidUsage);
                return Ok("ERR_USAGE_NOT_FOUND_IN_DB");
            }

            _labelTemplateProcessor.ProcessTemplateMontage(ordreFabrication, usageCorrespondanceProduit, uidOperateur, date, _configuration);
            int res = _labelsCreator.ProcessAndCreateLabelsMontage(date, uidOperateur, ordreFabrication, usageCorrespondanceProduit);

            if (res == -1)
            {
                Log.Warning("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
                return Ok("ERR_ETI_ALREADY_IN_DB");
            }

            return Ok("SUCCESS");
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult PrintEmballage(string uidOperateur, string uidOrdreFabrication, string uidUsage)
        {

            Log.Information("GenerateTemplate called with uidOperateur: {uidOrdreFabrication}", uidOrdreFabrication);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            OrdreFabrication ordreFabrication = _apiData.GetOrdreFabricationWithUidOF(uidOrdreFabrication).Result;

            if (ordreFabrication == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", uidOrdreFabrication);
                return Ok("ERR_OF_NOT_FOUND_IN_DB");
            }

            UsageCorrespondanceProduit usageCorrespondanceProduit = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.UidProduit, uidUsage).Result;

            if (usageCorrespondanceProduit == null)
            {
                Log.Warning("USageCorrespondance not found for of: {uidProduit} and usage: {uidUsage}", ordreFabrication.UidProduit, uidUsage);
                return Ok("ERR_USAGE_NOT_FOUND_IN_DB");
            }

            _labelTemplateProcessor.ProcessTemplateEmballage(ordreFabrication, usageCorrespondanceProduit, uidOperateur, date, _configuration);
            int res = _labelsCreator.ProcessAndCreateLabelsEmballage(date, uidOperateur, ordreFabrication, usageCorrespondanceProduit);

            if (res == -1)
            {
                Log.Warning("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
                return Ok("ERR_ETI_ALREADY_IN_DB");
            }

            return Ok("SUCCESS");
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult PrintAccessoire(string uidOperateur, string uidOrdreFabrication, string uidUsage)
        {

            Log.Information("GenerateTemplate called with uidOperateur: {uidOrdreFabrication}", uidOrdreFabrication);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            OrdreFabrication ordreFabrication = _apiData.GetOrdreFabricationWithUidOF(uidOrdreFabrication).Result;

            if (ordreFabrication == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", uidOrdreFabrication);
                return Ok("ERR_OF_NOT_FOUND_IN_DB");
            }

            UsageCorrespondanceProduit usageCorrespondanceProduit = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.UidProduit, uidUsage).Result;

            if (usageCorrespondanceProduit == null)
            {
                Log.Warning("USageCorrespondance not found for of: {uidProduit} and usage: {uidUsage}", ordreFabrication.UidProduit, uidUsage);
                return Ok("ERR_USAGE_NOT_FOUND_IN_DB");
            }

            _labelTemplateProcessor.ProcessTemplateAccessoire(ordreFabrication, usageCorrespondanceProduit, uidOperateur, date, _configuration);
            int res = _labelsCreator.ProcessAndCreateLabelsAccessoire(date, uidOperateur, ordreFabrication, usageCorrespondanceProduit);

            if (res == -1)
            {
                Log.Warning("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
                return Ok("ERR_ETI_ALREADY_IN_DB");
            }

            return Ok("SUCCESS");
        }
        */

        /// <summary>
        /// Méthode Reprint responsable de la réimpression d'étiquette de production.
        /// </summary>
        /// <param name="uidAuthentiEtiquette"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult Reprint(string uidAuthentiEtiquette)
        {
            Log.Information("Reprint called with uidAuthentiEtiquette: {uidAuthentiEtiquette}", uidAuthentiEtiquette);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            Label etiquette = _apiData.GetEtiquetteByUidAuthenti(uidAuthentiEtiquette).Result;
           
            if (etiquette == null)
            {
                Log.Warning("Etiquette not found for uidAuthentiEtiquette: {uidAuthentiEtiquette}", uidAuthentiEtiquette);
                return Ok("ERR_ETIQ_NOT_FOUND");
            }

            ManufacturingOrder ordre = _apiData.GetOrdreFabricationWithUidOF(etiquette.ManufacturingOrderUid).Result;

            if (ordre == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", etiquette.ManufacturingOrderUid);
                return Ok("ERR_ORDER_NOT_FOUND");
            }

            Template usage = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordre.ProductUid, etiquette.Usage).Result;

            if (usage == null)
            {
                Log.Warning("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordre.ProductUid, etiquette.Usage);
                return Ok("ERR_USAGE_NOT_FOUND");
            }

            ArticleVersion version = _apiData.GetVersionWithUidArticle(ordre.ProductUid).Result;

            if (version == null)
            {
                Log.Warning("VersionArticle not found for uidProduit: {uidProduit}", ordre.ProductUid);
                return Ok("ERR_VERSION_NOT_FOUND");
            }

            Article article = _apiData.GetArticleByUidArticle(ordre.ProductUid).Result;

            if (article == null)
            {
                Log.Warning("Article not found for uidProduit: {uidProduit}", ordre.ProductUid);
                return Ok("ERR_ARTICLE_NOT_FOUND");
            }
            try
            {
                _labelTemplateProcessor.ProcessTemplateReprint(etiquette, ordre, usage, version, article, _configuration);
                Log.Information("Reprint action called with " + usage.Usage + " ID");

                _printer.ExecutePrinting(usage.Usage);
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString(), ex);
            }


            Log.Information("Reprint successful for uidAuthentiEtiquette: {uidAuthentiEtiquette}", uidAuthentiEtiquette);
            return Ok("SUCCESS");
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult ReprintEntireLot(string ordreFabrication, string usageEti, string uidOperateur)
        {
            Log.Information("ReprintEntireLot called with ordreFab: {ordreFabrication} et usage : {Usage}", ordreFabrication, usageEti);

            string date = DateTime.Now.ToString("dd/MM/yyyy");

            /*
            Etiquette etiquette = _apiData.GetEtiquetteByUidAuthenti(uidAuthentiEtiquette).Result;
            Log.Debug("Etiquette => " + etiquette.ToString());
            */

            ManufacturingOrder ordre = _apiData.GetOrdreFabricationWithUidOF(ordreFabrication).Result;

            if (ordre == null)
            {
                Log.Warning("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", ordreFabrication);
                return Ok("ERR_ORDER_NOT_FOUND");
            }

            Template usage = _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordre.ProductUid, usageEti).Result;

            if (usage == null)
            {
                Log.Warning("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordre.ProductUid, usageEti);
                return Ok("ERR_USAGE_NOT_FOUND");
            }

            ArticleVersion version = _apiData.GetVersionWithUidArticle(ordre.ProductUid).Result;

            if (version == null)
            {
                Log.Warning("VersionArticle not found for uidProduit: {uidProduit}", ordre.ProductUid);
                return Ok("ERR_VERSION_NOT_FOUND");
            }

            Article article = _apiData.GetArticleByUidArticle(ordre.ProductUid).Result;

            if (article == null)
            {
                Log.Warning("Article not found for uidProduit: {uidProduit}", ordre.ProductUid);
                return Ok("ERR_ARTICLE_NOT_FOUND");
            }

            try
            {
                _labelTemplateProcessor.ProcessTemplateCommon(ordre, version, usage, date, uidOperateur ,article, _configuration);
                Log.Information("Reprint action called with " + usage.Usage + " ID");

                _printer.ExecutePrinting(usage.Usage);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), ex);
            }

            Log.Information("Reprint successful Entire lot");
            return Ok("SUCCESS");
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PrintArtBorne(string articleUid, string quantity)
        {
            try
            {
                Article art = await _apiData.GetArticleByUidArticle(articleUid);
                _labelTemplateProcessor.ProcessTemplateArtBorne(art, quantity, _configuration);
                _printer.PrintFtp();

                return Ok(art);
            }
            catch (Exception ex)
            {
                Log.Error("PrintArtBorne gone wrong.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult ErrorPrinter()
        {
            Log.Error("L'imprimante n'a pas sorti les étiquettes");

            return Ok("");
        }
    }
}
