using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Convocation_Management_System.Web.UI.Models
{
    public class StaffTaskViewModel
    {
        public int StaffTaskId { get; set; }
        public int DistributionTaskId { get; set; }
        public List<SelectListItem> DistributionTasks { get; set; } = new();

        public int UserAccountId { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime? AssignedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public List<SelectListItem> staffs{ get; set; } = new();
    }
}