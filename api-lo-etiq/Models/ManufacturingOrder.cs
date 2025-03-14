using System;
using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class ManufacturingOrder
    {
        [Key]
        public  string ManufacturingOrderUid { get; set; }
        public  string ProductUid { get; set; }
        public  string Quantity { get; set; }
        public  string FirstSerialNumber { get; set; }
        public  string InternalOrderNumber { get; set; }
        public  string ExternalOrderNumber { get; set; }


    }
}
