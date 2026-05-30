using System.ComponentModel.DataAnnotations;


namespace Convocation.Entities
{
    public class DistributionTask
    {
        [Key]
        public int DistributionTaskId { get; set; }

        // =========================
        // EVENT
        // =========================

        public int EventId { get; set; }
        public Event? Event { get; set; }

        // =========================
        // TASK INFO
        // =========================

        [Required]
        [StringLength(150)]
        public string TaskTitle { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Food / Gown / Certificate / Kit
        [Required]
        public string DistributionType { get; set; } = string.Empty;

        // =========================
        // STATUS
        // =========================

        public string Status { get; set; } = "Pending";
        // Pending / In Progress / Completed

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        public string? Remarks { get; set; }
    }
}