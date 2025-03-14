using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class Article
    {
        [Key]
        public  string ArticleUid { get; set; }
        public  string Designation { get; set; }
        public  string Store { get; set; }
        public  string Family { get; set; }
        public  string Packaging { get; set; }
        public  string ManageStock { get; set; }
        public  string SupplierReference { get; set; }
        public  string GpaoQuantity { get; set; }
    }
}
