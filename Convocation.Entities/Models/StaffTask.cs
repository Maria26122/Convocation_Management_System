using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class StaffTask
    {
        [Key]
        public int StaffTaskId { get; set; }

        // =========================
        // RELATION: DISTRIBUTION TASK
        // =========================
        [Required]
        public int DistributionTaskId { get; set; }

        [ForeignKey(nameof(DistributionTaskId))]
        public DistributionTask DistributionTask { get; set; }

        // =========================
        // ASSIGNED STAFF
        // =========================
        [Required]
        public int UserAccountId { get; set; }

        [ForeignKey(nameof(UserAccountId))]
        public UserAccount UserAccount { get; set; }

        // =========================
        // STATUS TRACKING
        // =========================
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

      
    }
}