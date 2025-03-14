using System.ComponentModel.DataAnnotations;

namespace api_lo_etiq.Models
{
    public class User
    {
        [Key]
        public  string UserUid { get; set; }
        public  string LastName { get; set; }
        public  string FirstName { get; set; }
        public  string EntryDate { get; set; }
        public  string ExitDate { get; set; }
        public  string Department { get; set; }
    }
}
