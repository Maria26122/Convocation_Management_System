using System;
using System.Collections.Generic;
using System.Text;

namespace Convocation_Management_System.Web.UI.Models
{
    public class EventManagerDashboardViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";

        public int TotalEvents { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedTasks { get; set; }

        public int TotalStaff { get; set; }
    }
}