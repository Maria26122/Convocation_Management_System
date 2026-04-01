namespace Convocation_Management_System.Web.UI.Models
{
    public class AdminReportsViewModel
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

        public List<EventReportItem> EventReports { get; set; } = new();
    }

    public class EventReportItem
    {
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int RegistrationCount { get; set; }
        public int GuestCount { get; set; }
        public decimal TotalCollectedAmount { get; set; }
    }
}