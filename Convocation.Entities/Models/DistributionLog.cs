namespace Convocation.Entities
{
    public class DistributionLog
    {
        public int DistributionLogId { get; set; }

        public int RegistrationId { get; set; }
        public Registration Registration { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }

        public Event? Event { get; set; }
        public int EventId { get; set; } 

        public Participant? Participant { get; set; }
        public int ParticipantId { get; set; }
        public string ActionType { get; set; }

        public DateTime ActionDate { get; set; }

        public string? Note { get; set; }

        public string? Remarks { get; set; }
    }
}