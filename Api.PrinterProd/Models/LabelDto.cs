using System.ComponentModel.DataAnnotations;

namespace Api.PrinterProd.Models
{
    public class LabelDto
    {
        [Key]
        public required string AuthentiUid { get; set; }
        public required string UserUid { get; set; }
        public required string ProductUid { get; set; }
        public required int ManufacturingOrderUid { get; set; }
        public required DateTimeOffset CreationDate { get; set; }
        public required int CurrentSerialNumber { get; set; }
        public required string ProductVersion { get; set; }
        public required string LabelTypeUid { get; set; }
        public required string Template { get; set; }
    }
}
