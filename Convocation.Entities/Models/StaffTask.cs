using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class StaffTask
    {
        [Key]
        public int StaffTaskId { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }

        [Required]
        [StringLength(150)]
        public string TaskTitle { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";
        // Pending, InProgress, Completed

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        public string? Remarks { get; set; }
    }
}