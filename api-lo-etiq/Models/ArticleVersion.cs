using System;
using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class ArticleVersion
    {
        [Key]
        public  string ArticleVersionUid { get; set; }
        public  string ArticleUid { get; set; }
        public  string FirmwareVersion { get; set; }
    }
}

