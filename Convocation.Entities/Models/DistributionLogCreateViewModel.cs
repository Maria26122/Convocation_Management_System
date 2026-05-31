using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Convocation_Management_System.Web.UI.Models
{
    public class DistributionLogCreateViewModel
    {
        public int DistributionLogId { get; set; }

        // =========================
        // REGISTRATION
        // =========================

        [Required]
        [Display(Name = "Registration")]
        public int RegistrationId { get; set; }

        [Required]
        [Display(Name = "Participant")]
        public int ParticipantId { get; set; }

        [Required]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        // =========================
        // DISTRIBUTION
        // =========================

        [Required]
        [Display(Name = "Distribution Type")]
        public string ActionType { get; set; } = string.Empty;

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;

        // =========================
        // QR / STAFF
        // =========================

        public bool IsQrVerified { get; set; } = true;

        public int UserAccountId { get; set; }

        // =========================
        // DROPDOWNS
        // =========================

        public List<SelectListItem> Participants { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> Events { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> Registrations { get; set; }
            = new List<SelectListItem>();

        
    }
}
