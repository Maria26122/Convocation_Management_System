using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Convocation_Management_System.Web.UI.Models
{
    public class StaffTaskViewModel
    {
        public int StaffTaskId { get; set; }

        public int UserAccountId { get; set; }

        public string TaskTitle { get; set; }

        public string Description { get; set; }

        public string Status { get; set; } = "Pending";

        public string? Remarks { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public List<SelectListItem> Users { get; set; } = new();
    }
}