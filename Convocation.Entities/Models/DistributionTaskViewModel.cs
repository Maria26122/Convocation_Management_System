using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Convocation_Management_System.Web.UI.Models
{
    public class DistributionTaskViewModel
    {
        public int DistributionTaskId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public string TaskTitle { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string DistributionType { get; set; } = string.Empty;

        public int? AssignedStaffId { get; set; }

        public string Status { get; set; } = "Pending";

        public string? Remarks { get; set; }

        public List<SelectListItem> Events { get; set; } = new();

        public List<SelectListItem> Staffs { get; set; } = new();
    }
}