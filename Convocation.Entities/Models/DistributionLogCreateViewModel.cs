using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Convocation_Management_System.Web.UI.Models
{
    public class DistributionLogCreateViewModel
    {
        [Required]
        public int ParticipantId { get; set; }

        [Required]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "Distributed By")]
        public string? DistributedBy { get; set; }

        public string? Remarks { get; set; }

        public List<SelectListItem> Participant { get; set; } = new List<SelectListItem>();
    }
}