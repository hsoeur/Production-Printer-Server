using DataLib;
using System.Threading.Tasks;

namespace Api.PrinterProd.Services.Labels
{
    public interface ILabelsCreator
    {
        Task<int> ProcessAndCreateLabelsCommon(DateTimeOffset date, string uidOperateur, ManufacturingOrder? ordreFabrication, LabelTemplate? usageTemplate, FirmwareVersion? version);
        int ProcessAndCreateLabelsPartialPrint(DateTimeOffset date, string userUid, ManufacturingOrder ordreFabrication, LabelTemplate usageCorrespondanceProduit, FirmwareVersion versionArticle, int printQuantity);
    }
}
