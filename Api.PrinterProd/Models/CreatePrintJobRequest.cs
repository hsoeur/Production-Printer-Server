using System.Text.Json.Serialization;

namespace Api.PrinterProd.Models;

public sealed class CreatePrintJobRequest
{
    public string JobId { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int ManufacturingOrderUid { get; init; }
    public string ProductUid { get; init; } = string.Empty;
    public string UidUsage { get; init; } = string.Empty;
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SerialNumber { get; init; }
}

public sealed class PrintJobResponse
{
    public string JobId { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
