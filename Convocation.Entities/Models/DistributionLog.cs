using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class DistributionLog
    {
        [Key]
        public int DistributionLogId { get; set; }

        [Required]
        public int ParticipantId { get; set; }

        // Optional link to a registration (some views/controllers expect this)
        public int? RegistrationId { get; set; }

        [ForeignKey("RegistrationId")]
        public virtual Registration? Registration { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        public DateTime DistributedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? DistributedBy { get; set; }

        [StringLength(250)]
        public string? Remarks { get; set; }

        // Optional action fields (used by some existing views)
        [StringLength(50)]
        public string? ActionType { get; set; }

        public DateTime? ActionDate { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }

        // Optional staff/user who performed the action
        public int? UserAccountId { get; set; }

        [ForeignKey("UserAccountId")]
        public virtual UserAccount? UserAccount { get; set; }

        [ForeignKey("ParticipantId")]
        public virtual Participant? Participant { get; set; }
    }
}