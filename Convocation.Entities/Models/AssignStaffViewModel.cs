using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Convocation_Management_System.Web.UI.Models
{
    public class AssignStaffViewModel
    {
        public int TaskId { get; set; }

        public int SelectedUserId { get; set; }

        public List<SelectListItem> StaffList { get; set; } = new();
    }
}