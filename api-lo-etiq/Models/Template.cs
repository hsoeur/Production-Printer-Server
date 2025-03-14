using System;
using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class Template
    {
        [Key]
        public  string TemplateUid { get; set; }
        public  string Product { get; set; }
        public  string Usage { get; set; }
        public  string TemplateFile { get; set; }
        public  string Photo { get; set; }
        public  string StartDate { get; set; }
        public  string EndDate { get; set; }
        public  string UserUid { get; set; }
    }
}
