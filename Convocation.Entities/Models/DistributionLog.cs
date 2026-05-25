using System;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class DistributionLog
    {
        [Key]
        public int DistributionLogId { get; set; }

        // =========================
        // RELATIONS
        // =========================

        public int RegistrationId { get; set; }
        public Registration? Registration { get; set; }

        public int ParticipantId { get; set; }
        public Participant? Participant { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }

        // Staff who distributed
        public int UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }

        // =========================
        // TASK LINK
        // =========================

        public int? DistributionTaskId { get; set; }
        public DistributionTask? DistributionTask { get; set; }

        // =========================
        // DISTRIBUTION INFO
        // =========================

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = string.Empty;
        // Food / Gown / Kit / Certificate

        public DateTime ActionDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Note { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsDelivered { get; set; } = true;

        public bool IsQrVerified { get; set; } = false;
    }
}