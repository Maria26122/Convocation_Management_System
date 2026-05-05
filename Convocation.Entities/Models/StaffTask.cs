using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class StaffTask
    {
        [Key]
        public int StaffTaskId { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public int StaffUserAccountId { get; set; }
        public UserAccount? StaffUserAccount { get; set; }

        [Required]
        [StringLength(100)]
        public string TaskName { get; set; } = "";

        [StringLength(300)]
        public string? Description { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Assigned";

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }
    }
}