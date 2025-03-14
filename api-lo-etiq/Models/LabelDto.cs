using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace api_lo_etiq.Models
{
    public class LabelDto
    {
        public  string AuthentiUid { get; set; }
        public  string UserUid { get; set; }
        public  string ProductUid { get; set; }
        public  string ManufacturingOrderUid { get; set; }
        public  string CreationDate { get; set; }
        public  string CurrentSerialNumber { get; set; }
        public  string ProductVersion { get; set; }
        public  string Usage { get; set; }
        public  string Template { get; set; }

    }
}
