using System.ComponentModel.DataAnnotations;

namespace Convocation_Management_System.Web.UI.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        public string? Phone { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        public string StudentId { get; set; } = "";

        [Required]
        public string Department { get; set; } = "";

        [Required]
        public string Program { get; set; } = "";

        [Required]
        public string Session { get; set; } = "";
    }
}