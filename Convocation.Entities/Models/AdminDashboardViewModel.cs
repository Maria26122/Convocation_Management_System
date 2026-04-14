namespace Convocation_Management_System.Web.UI.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalParticipants { get; set; }
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalGuests { get; set; }
        public int TotalPayments { get; set; }
        public int TotalQrPasses { get; set; }
        public int TotalDistributionLogs { get; set; }

        public int ApprovedRegistrations { get; set; }
        public int PendingRegistrations { get; set; }
        public int RejectedRegistrations { get; set; }

        public int PaidPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
    }


}