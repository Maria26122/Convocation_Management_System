using System;
using System.Collections.Generic;
using System.Text;

namespace Convocation_Management_System.Web.UI.Models
{
    public class OperationDashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }

        public int TotalDistributedToday { get; set; }
        public int PendingDistribution { get; set; }

        public int FoodCount { get; set; }
        public int GownCount { get; set; }
        public int CertificateCount { get; set; }
        public int KitCount { get; set; }

        public int ActiveStaff { get; set; }
    }
}
