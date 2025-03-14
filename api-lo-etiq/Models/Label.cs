using System;
using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class Label
    {
        [Key]
        public  string LabelUid { get; set; }
        public  string AuthentiUid { get; set; }
        public  string UserUid { get; set; }
        public  string ProductUid { get; set; }
        public  string ManufacturingOrderUid { get; set; }
        public  string CreationDate { get; set; }
        public  string CurrentSerialNumber { get; set; }
        public  string ProductVersion { get; set; }
        public  string Usage { get; set; }
        public  string Template { get; set; }

        public override string ToString()
        {
            return $"Label [LabelUid={LabelUid}, AuthentiUid={AuthentiUid}, UserUid={UserUid}, ProductUid={ProductUid}, " +
                   $"ManufacturingOrderUid={ManufacturingOrderUid}, CreationDate={CreationDate}, " +
                   $"CurrentSerialNumber={CurrentSerialNumber}, ProductVersion={ProductVersion}, " +
                   $"Usage={Usage}, Template={Template}]";
        }
    }
}
