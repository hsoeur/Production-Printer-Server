using Api.PrinterProd.Services.api_data;
using Api.PrinterProd.Services.Labels;
using Api.PrinterProd.Services.Printer;
using Api.PrinterProd.Services.Template;
using DataLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.PrinterProd.Controllers
{
    /// <summary>
    /// CLasse PrintController qui expose des endpoints pour l'impression et la réimpression des étiquettes de production.
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Produces("application/json")]
    public class PrintController : ControllerBase
    {
        private readonly IApiData _apiData;
        private readonly ILabelTemplateProcessor _labelTemplateProcessor;
        private readonly IConfiguration _configuration;
        private readonly ILabelsCreator _labelsCreator;
        private readonly IPrinter _printer;

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

        [SwaggerOperation(
        Summary = "Test d'impression de fichier",
        Description = "Ce endpoint permet de tester l'impression d'un fichier d'étiquette.",
        OperationId = "PrintFile"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PrintFile()
        {
            try
            {
                _printer.PrintFileTest("Etiquette Lot", "out_file.txt");
                Log.Information("Impression Terminée avec Succès");
                return Ok("Impression Successful.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de PrintFile");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne",
                    Detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Méthode PrintCommon responsable de l'impression normale des étiquettes de production.
        /// </summary>
        [SwaggerOperation(
        Summary = "Impression normale des étiquettes de production",
        Description = "Ce endpoint permet d'imprimer les étiquettes de production en fonction d'un ordre de fabrication, d'un opérateur et d'un usage spécifique.",
        OperationId = "PrintCommon"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PrintCommon([FromQuery] string uidOperateur, [FromQuery] int uidOrdreFabrication, [FromQuery] string uidUsage)
        {
            if (string.IsNullOrWhiteSpace(uidOperateur) || string.IsNullOrWhiteSpace(uidUsage) || uidOrdreFabrication <= 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre(s) invalide(s)",
                    Detail = "Les paramètres 'uidOperateur', 'uidOrdreFabrication' et 'uidUsage' sont requis."
                });
            }

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                Log.Information("PrintCommon called with uidOperateur={uidOperateur}, uidOrdreFabrication={uidOrdreFabrication}, uidUsage={uidUsage}", uidOperateur, uidOrdreFabrication, uidUsage);
                DateTimeOffset date = DateTimeOffset.Now;

                var ordreFabrication = await _apiData.GetOrdreFabricationWithUidOF(uidOrdreFabrication);
                if (ordreFabrication == null)
                {
                    Log.Information("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", uidOrdreFabrication);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Ordre de fabrication introuvable",
                        Detail = $"Aucun ordre de fabrication trouvé pour l'identifiant {uidOrdreFabrication}."
                    });
                }

                if (ordreFabrication.FirstSerialNumber < 100001 && ordreFabrication.FirstSerialNumber != 0)
                {
                    Log.Information("Serial number of OF {uidOrdreFabrication} is less than100001", uidOrdreFabrication);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Numéro de série invalide",
                        Detail = "Le numéro de série de l'ordre de fabrication est inférieur à100001."
                    });
                }

                var usageCorrespondanceProduit = await _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.ProductUid, uidUsage);
                if (usageCorrespondanceProduit == null)
                {
                    Log.Information("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordreFabrication.ProductUid, uidUsage);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Usage introuvable",
                        Detail = $"Aucun usage trouvé pour le produit {ordreFabrication.ProductUid} et usage {uidUsage}."
                    });
                }

                var versionArticle = await _apiData.GetVersionWithUidArticle(ordreFabrication.ProductUid);
                if (versionArticle == null)
                {
                    /*
                    Log.Information("VersionArticle not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Version introuvable",
                        Detail = $"Aucune version trouvée pour le produit {ordreFabrication.ProductUid}."
                    });
                    */

                    versionArticle = new FirmwareVersion() { FirmwareVersionUid = Guid.NewGuid().ToString(), ArticleUid = ordreFabrication.ProductUid, Version = "Vxx.xx" };
                }

                var article = await _apiData.GetArticleByUidArticle(ordreFabrication.ProductUid);
                if (article == null)
                {
                    Log.Information("Article not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Article introuvable",
                        Detail = $"Aucun article trouvé pour le produit {ordreFabrication.ProductUid}."
                    });
                }

                _labelTemplateProcessor.ProcessTemplateCommon(ordreFabrication,
                    versionArticle,
                    usageCorrespondanceProduit,
                    date,
                    uidOperateur,
                    article,
                    _configuration,
                    outputFile.Path);

                int res = await _labelsCreator.ProcessAndCreateLabelsCommon(date,
                    uidOperateur,
                    ordreFabrication,
                    usageCorrespondanceProduit,
                    versionArticle,
                    outputFile.Path);

                if (res == -1)
                {
                    Log.Information("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", uidOrdreFabrication, uidOperateur);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Étiquette déjà existante",
                        Detail = "L'étiquette existe déjà en base de données."
                    });
                }
                return Ok("SUCCESS");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de PrintCommon");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne",
                    Detail = ex.Message
                });
            }
        }

        [SwaggerOperation(
        Summary = "Impression partielle d'étiquettes",
        Description = "Ce endpoint permet d'imprimer une quantité partielle d'étiquettes pour un ordre de fabrication donné.",
        OperationId = "PartialPrint"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PartialPrint([FromQuery] string userUid, [FromQuery] int manufacturingOrderuid, [FromQuery] int printQuantity)
        {
            if (string.IsNullOrWhiteSpace(userUid) || manufacturingOrderuid <= 0 || printQuantity <= 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre(s) invalide(s)",
                    Detail = "Les paramètres 'userUid', 'manufacturingOrderuid' et 'printQuantity' sont requis et doivent être valides."
                });
            }

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                DateTimeOffset date = DateTimeOffset.Now;
                string usage = "Etiquette Lot";

                var ordreFabrication = await _apiData.GetOrdreFabricationWithUidOF(manufacturingOrderuid);
                if (ordreFabrication == null)
                {
                    Log.Information("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", manufacturingOrderuid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Ordre de fabrication introuvable",
                        Detail = $"Aucun ordre de fabrication trouvé pour l'identifiant {manufacturingOrderuid}."
                    });
                }

                if (ordreFabrication.FirstSerialNumber < 100001 && ordreFabrication.FirstSerialNumber != 0)
                {
                    Log.Information("Serial number of OF {uidOrdreFabrication} is less than100001", manufacturingOrderuid);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Numéro de série invalide",
                        Detail = "Le numéro de série de l'ordre de fabrication est inférieur à100001."
                    });
                }

                var usageCorrespondanceProduit = await _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.ProductUid, usage);
                if (usageCorrespondanceProduit == null)
                {
                    Log.Information("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordreFabrication.ProductUid, usage);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Usage introuvable",
                        Detail = $"Aucun usage trouvé pour le produit {ordreFabrication.ProductUid} et usage {usage}."
                    });
                }

                var versionArticle = await _apiData.GetVersionWithUidArticle(ordreFabrication.ProductUid);
                if (versionArticle == null)
                {
                    Log.Information("VersionArticle not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Version introuvable",
                        Detail = $"Aucune version trouvée pour le produit {ordreFabrication.ProductUid}."
                    });
                }

                var article = await _apiData.GetArticleByUidArticle(ordreFabrication.ProductUid);
                if (article == null)
                {
                    Log.Information("Article not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Article introuvable",
                        Detail = $"Aucun article trouvé pour le produit {ordreFabrication.ProductUid}."
                    });
                }

                _labelTemplateProcessor.ProcessTemplatePartialPrint(ordreFabrication, versionArticle, usageCorrespondanceProduit, date, userUid, article, printQuantity, _configuration, outputFile.Path);
                int res = _labelsCreator.ProcessAndCreateLabelsPartialPrint(date, userUid, ordreFabrication, usageCorrespondanceProduit, versionArticle, printQuantity, outputFile.Path);

                if (res == -1)
                {
                    Log.Information("Label already in database for uidOrdreFabrication: {uidOrdreFabrication}, uidOperateur: {uidOperateur}", manufacturingOrderuid, userUid);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Étiquette déjà existante",
                        Detail = "L'étiquette existe déjà en base de données."
                    });
                }

                return Ok("SUCCESS");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de PartialPrint");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne",
                    Detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Méthode Reprint responsable de la réimpression d'étiquette de production.
        /// </summary>
        [SwaggerOperation(
        Summary = "Réimpression d'une étiquette de production",
        Description = "Ce endpoint permet de réimprimer une étiquette de production existante en utilisant son identifiant d'authentification.",
        OperationId = "Reprint"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Reprint([FromQuery] string uidAuthentiEtiquette)
        {
            if (string.IsNullOrWhiteSpace(uidAuthentiEtiquette))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre invalide",
                    Detail = "Le paramètre 'uidAuthentiEtiquette' est requis."
                });
            }

            Log.Information("Reprint called with uidAuthentiEtiquette: {uidAuthentiEtiquette}", uidAuthentiEtiquette);

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                var etiquette = await _apiData.GetEtiquetteByUidAuthenti(uidAuthentiEtiquette);
                if (etiquette == null)
                {
                    Log.Information("Etiquette not found for uidAuthentiEtiquette: {uidAuthentiEtiquette}", uidAuthentiEtiquette);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Étiquette introuvable",
                        Detail = $"Aucune étiquette trouvée pour l'identifiant {uidAuthentiEtiquette}."
                    });
                }

                var ordre = await _apiData.GetOrdreFabricationWithUidOF(etiquette.ManufacturingOrderUid);
                if (ordre == null)
                {
                    Log.Information("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", etiquette.ManufacturingOrderUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Ordre de fabrication introuvable",
                        Detail = $"Aucun ordre de fabrication trouvé pour l'identifiant {etiquette.ManufacturingOrderUid}."
                    });
                }

                var usage = await _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordre.ProductUid, etiquette.LabelTypeUid);
                if (usage == null)
                {
                    Log.Information("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordre.ProductUid, etiquette.LabelTypeUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Usage introuvable",
                        Detail = $"Aucun usage trouvé pour le produit {ordre.ProductUid} et usage {etiquette.LabelTypeUid}."
                    });
                }

                var version = await _apiData.GetVersionWithUidArticle(ordre.ProductUid);
                if (version == null)
                {
                    Log.Information("VersionArticle not found for uidProduit: {uidProduit}, using default version", ordre.ProductUid);
                    version = new FirmwareVersion()
                    {
                        FirmwareVersionUid = Guid.NewGuid().ToString(),
                        ArticleUid = ordre.ProductUid,
                        Version = "Vxx.xx"
                    };
                }

                var article = await _apiData.GetArticleByUidArticle(ordre.ProductUid);
                if (article == null)
                {
                    Log.Information("Article not found for uidProduit: {uidProduit}", ordre.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Article introuvable",
                        Detail = $"Aucun article trouvé pour le produit {ordre.ProductUid}."
                    });
                }

                try
                {
                    _labelTemplateProcessor.ProcessTemplateReprint(etiquette, ordre, usage, version, article, _configuration, outputFile.Path);
                    Log.Information("Reprint action called with {LabelTypeUid} ID", usage.LabelTypeUid);
                    _printer.PrintFileTest(usage.LabelTypeUid, outputFile.Path);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erreur lors du traitement de la réimpression");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Erreur lors du traitement de la réimpression",
                        Detail = ex.Message
                    });
                }

                Log.Information("Reprint successful for uidAuthentiEtiquette: {uidAuthentiEtiquette}", uidAuthentiEtiquette);
                return Ok("SUCCESS");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de Reprint");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne",
                    Detail = ex.Message
                });
            }
        }

        [SwaggerOperation(
        Summary = "Réimpression d'un lot entier d'étiquettes",
        Description = "Ce endpoint permet de réimprimer toutes les étiquettes d'un lot complet en fonction d'un ordre de fabrication et d'un usage.",
        OperationId = "ReprintEntireLot"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReprintEntireLot([FromQuery] int ordreFabrication, [FromQuery] string usageEti, [FromQuery] string uidOperateur)
        {
            if (ordreFabrication <= 0 || string.IsNullOrWhiteSpace(usageEti) || string.IsNullOrWhiteSpace(uidOperateur))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre(s) invalide(s)",
                    Detail = "Les paramètres 'ordreFabrication', 'usageEti' et 'uidOperateur' sont requis et doivent être valides."
                });
            }

            Log.Information("ReprintEntireLot called with ordreFab: {ordreFabrication} et usage : {Usage}", ordreFabrication, usageEti);
            DateTimeOffset date = DateTimeOffset.Now;

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                var ordre = await _apiData.GetOrdreFabricationWithUidOF(ordreFabrication);
                if (ordre == null)
                {
                    Log.Information("OrdreFabrication not found for uidOrdreFabrication: {uidOrdreFabrication}", ordreFabrication);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Ordre de fabrication introuvable",
                        Detail = $"Aucun ordre de fabrication trouvé pour l'identifiant {ordreFabrication}."
                    });
                }

                var usage = await _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordre.ProductUid, usageEti);
                if (usage == null)
                {
                    Log.Information("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", ordre.ProductUid, usageEti);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Usage introuvable",
                        Detail = $"Aucun usage trouvé pour le produit {ordre.ProductUid} et usage {usageEti}."
                    });
                }

                var version = await _apiData.GetVersionWithUidArticle(ordre.ProductUid);
                if (version == null)
                {
                    Log.Information("VersionArticle not found for uidProduit: {uidProduit}, using default version", ordre.ProductUid);
                    version = new FirmwareVersion()
                    {
                        FirmwareVersionUid = Guid.NewGuid().ToString(),
                        ArticleUid = ordre.ProductUid,
                        Version = "Vxx.xx"
                    };
                }

                var article = await _apiData.GetArticleByUidArticle(ordre.ProductUid);
                if (article == null)
                {
                    Log.Information("Article not found for uidProduit: {uidProduit}", ordre.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Article introuvable",
                        Detail = $"Aucun article trouvé pour le produit {ordre.ProductUid}."
                    });
                }

                try
                {
                    _labelTemplateProcessor.ProcessTemplateCommon(ordre, version, usage, date, uidOperateur, article, _configuration, outputFile.Path);
                    Log.Information("Reprint action called with {LabelTypeUid} ID", usage.LabelTypeUid);
                    _printer.PrintFileTest(usage.LabelTypeUid, outputFile.Path);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erreur lors du traitement de la réimpression du lot entier");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Erreur lors du traitement de la réimpression du lot entier",
                        Detail = ex.Message
                    });
                }

                Log.Information("Reprint successful Entire lot");
                return Ok("SUCCESS");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de ReprintEntireLot");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne",
                    Detail = ex.Message
                });
            }
        }

        [SwaggerOperation(
        Summary = "Impression d'étiquettes avec initiales",
        Description = "Ce endpoint permet d'imprimer des étiquettes contenant les initiales d'un utilisateur pour une quantité spécifiée.",
        OperationId = "PrintInitials"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PrintInitials([FromQuery] string userUid, [FromQuery] string quantity)
        {
            if (string.IsNullOrWhiteSpace(userUid) || string.IsNullOrWhiteSpace(quantity))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre(s) invalide(s)",
                    Detail = "Les paramètres 'userUid' et 'quantity' sont requis."
                });
            }

            Log.Information("PrintInitials called with userUid: {userUid} and quantity: {quantity}", userUid, quantity);

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                var user = await _apiData.GetUserByUid(userUid);
                if (user == null)
                {
                    Log.Information("User not found for userUid: {userUid}", userUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Utilisateur introuvable",
                        Detail = $"Aucun utilisateur trouvé pour l'identifiant {userUid}."
                    });
                }

                string initials = $"{(string.IsNullOrEmpty(user.LastName) ? "" : user.LastName[0].ToString().ToUpper())}. " +
                $"{(string.IsNullOrEmpty(user.FirstName) ? "" : user.FirstName[0].ToString().ToUpper())} ({user.UserUid})";

                _labelTemplateProcessor.ProcessTemplateInitials(initials, quantity, _configuration, outputFile.Path);
                _printer.ExecutePrinting("Etiquette Lot", outputFile.Path);

                Log.Information("PrintInitials completed with initials: {initials}", initials);
                return Ok("SUCCESS");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur dans PrintInitials");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne lors de l'impression des initiales.",
                    Detail = ex.Message
                });
            }
        }

        [SwaggerOperation(
        Summary = "Impression d'étiquettes pour article borne",
        Description = "Ce endpoint permet d'imprimer des étiquettes pour un article destiné à une borne avec une quantité spécifiée.",
        OperationId = "PrintArtBorne"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Article))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PrintArtBorne([FromQuery] string articleUid, [FromQuery] string quantity)
        {
            if (string.IsNullOrWhiteSpace(articleUid) || string.IsNullOrWhiteSpace(quantity))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre(s) invalide(s)",
                    Detail = "Les paramètres 'articleUid' et 'quantity' sont requis."
                });
            }

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                var art = await _apiData.GetArticleByUidArticle(articleUid);
                if (art == null)
                {
                    Log.Information("Article not found for articleUid: {articleUid}", articleUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Article introuvable",
                        Detail = $"Aucun article trouvé pour l'identifiant {articleUid}."
                    });
                }
                _labelTemplateProcessor.ProcessTemplateArtBorne(art, quantity, _configuration, outputFile.Path);
                _printer.PrintFtp(outputFile.Path);
                return Ok(art);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de PrintArtBorne");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne lors de l'impression ArtBorne.",
                    Detail = ex.Message
                });
            }
        }

        [SwaggerOperation(
            Summary = "Signaler une erreur d'imprimante",
            Description = "Ce endpoint permet de signaler qu'une erreur d'impression s'est produite (imprimante n'a pas sorti les étiquettes).",
            OperationId = "ErrorPrinter"
            )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult ErrorPrinter()
        {
            Log.Error("L'imprimante n'a pas sorti les étiquettes");
            return Ok("");
        }

        /// <summary>
        /// Méthode PrintBySerialNumber responsable de l'impression d'une étiquette pour un numéro de série spécifique.
        /// </summary>
        [SwaggerOperation(
        Summary = "Impression d'étiquette par numéro de série",
        Description = "Ce endpoint permet d'imprimer une étiquette pour un produit spécifique en utilisant un numéro de série, un ordre de fabrication et un usage.",
        OperationId = "PrintBySerialNumber"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PrintBySerialNumber([FromQuery] int manufacturingOrderUid, [FromQuery] string productUid, [FromQuery] string uidUsage, [FromQuery] int serialNumber)
        {
            if (manufacturingOrderUid <= 0 || string.IsNullOrWhiteSpace(uidUsage) || serialNumber < 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Paramètre(s) invalide(s)",
                    Detail = "Les paramètres 'manufacturingOrderUid', 'uidUsage' et 'serialNumber' sont requis et doivent être valides."
                });
            }

            try
            {
                using var outputFile = TemporaryPrintFile.Create();
                Log.Information("PrintBySerialNumber called with manufacturingOrderUid={manufacturingOrderUid}, uidUsage={uidUsage}, serialNumber={serialNumber}",
                 manufacturingOrderUid, uidUsage, serialNumber);

                DateTimeOffset date = DateTimeOffset.Now;

                var ordreFabrication = await _apiData.GetOrdreFabricationWithUidOF(manufacturingOrderUid);
                if (ordreFabrication == null)
                {
                    Log.Information("OrdreFabrication not found for manufacturingOrderUid: {manufacturingOrderUid}", manufacturingOrderUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Ordre de fabrication introuvable",
                        Detail = $"Aucun ordre de fabrication trouvé pour l'identifiant {manufacturingOrderUid}."
                    });
                }

                // Validation du numéro de série
                if (serialNumber != 0 && serialNumber < 100001)
                {
                    Log.Information("Serial number {serialNumber} is less than 100001", serialNumber);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Numéro de série invalide",
                        Detail = "Le numéro de série doit être égal à 0 ou supérieur ou égal à 100001."
                    });
                }

                var usageCorrespondanceProduit = await _apiData.GetUsageCorrespondanceProduitWithUidProduitUidUsage(ordreFabrication.ProductUid, uidUsage);
                if (usageCorrespondanceProduit == null)
                {
                    Log.Information("UsageCorrespondanceProduit not found for uidProduit: {uidProduit} and uidUsage: {uidUsage}", productUid, uidUsage);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Usage introuvable",
                        Detail = $"Aucun usage trouvé pour le produit {productUid} et usage {uidUsage}."
                    });
                }

                var versionArticle = await _apiData.GetVersionWithUidArticle(ordreFabrication.ProductUid);
                if (versionArticle == null)
                {
                    Log.Information("VersionArticle not found for uidProduit: {uidProduit}, using default version", ordreFabrication.ProductUid);
                    versionArticle = new FirmwareVersion()
                    {
                        FirmwareVersionUid = Guid.NewGuid().ToString(),
                        ArticleUid = ordreFabrication.ProductUid,
                        Version = "Vxx.xx"
                    };
                }

                var article = await _apiData.GetArticleByUidArticle(ordreFabrication.ProductUid);
                if (article == null)
                {
                    Log.Information("Article not found for uidProduit: {uidProduit}", ordreFabrication.ProductUid);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Article introuvable",
                        Detail = $"Aucun article trouvé pour le produit {ordreFabrication.ProductUid}."
                    });
                }

                var kitTracea = await _apiData.GetKitTraceaByProductAndSerialNumber(productUid, serialNumber);
                if (kitTracea == null)
                {
                    Log.Information("KitTraceability not found for productUid: {productUid} and serialNumber: {serialNumber}", productUid, serialNumber);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Traçabilité kit introuvable",
                        Detail = $"Aucune traçabilité kit trouvée pour le produit {productUid} et le numéro de série {serialNumber}."
                    });
                }

                _labelTemplateProcessor.ProcessTemplateUnitSerial(ordreFabrication, versionArticle, usageCorrespondanceProduit, date, serialNumber, article, kitTracea, _configuration, outputFile.Path);

                // Impression via FTP pour la production
                bool printSuccess = _printer.PrintBySerialNumberFtp(outputFile.Path);
                if (!printSuccess)
                {
                    Log.Error("L'impression FTP a échoué pour le numéro de série: {serialNumber}", serialNumber);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Erreur d'impression",
                        Detail = "L'impression via FTP a échoué."
                    });
                }

                Log.Information("PrintBySerialNumber completed successfully for serial number: {serialNumber}", serialNumber);
                return Ok("SUCCESS");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de PrintBySerialNumber");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erreur interne",
                    Detail = ex.Message
                });
            }
        }

    }
}
