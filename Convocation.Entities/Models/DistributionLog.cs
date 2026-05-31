using System;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class DistributionLog
    {
        public int DistributionLogId { get; set; }

        public int RegistrationId { get; set; }
        public Registration? Registration { get; set; }

        public int ParticipantId { get; set; }
        public Participant? Participant { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }

        public int? DistributionTaskId { get; set; }
        public DistributionTask? DistributionTask { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;

        public string? Note { get; set; }

        public bool IsDelivered { get; set; } = true;

        public bool IsQrVerified { get; set; } = false;
    }
}