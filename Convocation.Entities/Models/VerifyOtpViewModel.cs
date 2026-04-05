using System.ComponentModel.DataAnnotations;

namespace Convocation_Management_System.Web.UI.Models
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "OTP Code")]
        public string OtpCode { get; set; } = string.Empty;
    }
}