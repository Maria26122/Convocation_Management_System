namespace Convocation_Management_System.Web.UI.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalParticipant { get; set; }
        public int TotalEvent { get; set; }
        public int TotalRegistration { get; set; }
        public int TotalGuest { get; set; }
        public int TotalPayment { get; set; }
        public int TotalQrPass { get; set; }
        public int TotalDistributionLog { get; set; }

        public int ApprovedRegistration { get; set; }
        public int PendingRegistration { get; set; }
        public int RejectedRegistration { get; set; }

        public int PaidPayment { get; set; }
        public int PendingPayment { get; set; }
        public int FailedPayment { get; set; }
    }


}