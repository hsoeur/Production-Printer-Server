using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class Usage
    {
        [Key]
        public string UsageUid { get; set; } = String.Empty;
    }
}
