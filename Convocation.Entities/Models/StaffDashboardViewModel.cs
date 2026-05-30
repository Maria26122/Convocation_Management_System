using Convocation.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Convocation_Management_System.Web.UI.Models
{
    public class StaffDashboardViewModel
    {
      
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";

        public int AssignedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }

        public int TodayScans { get; set; }

        public List<StaffTask> Tasks { get; set; } = new();
    }
}