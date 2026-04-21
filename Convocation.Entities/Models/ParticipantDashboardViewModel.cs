namespace Convocation_Management_System.Web.UI.Models
{
    public class ParticipantDashboardViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        public string StudentId { get; set; } = "";
        public string Department { get; set; } = "";
        public string Program { get; set; } = "";
        public string Session { get; set; } = "";
        public bool IsEligible { get; set; }

        public int ParticipantId { get; set; }

        public int? RegistrationId { get; set; }
        public string RegistrationStatus { get; set; } = "Not Registered";
        public DateTime? RegistrationDate { get; set; }
        public int GuestCount { get; set; }

        public string EventTitle { get; set; } = "No Event Assigned";
        public DateTime? EventDate { get; set; }
        public string Venue { get; set; } = "TBA";
        public int MaxGuestAllowed { get; set; }

        public string PaymentStatus { get; set; } = "Pending";
        public decimal PaidAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? TransactionId { get; set; }

        public bool HasQrPass { get; set; }
        public bool IsQrUsed { get; set; }
        public string QrCodeText { get; set; } = "";

        public int CompletionPercentage { get; set; }
    }
}