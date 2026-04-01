using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class Participant
    {
        [Key]
        public int ParticipantId { get; set; }

        [Required]
        public int UserAccountId { get; set; }

        [Required]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Program { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Session { get; set; } = string.Empty;

        public bool IsEligible { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserAccountId")]
        public virtual UserAccount? UserAccount { get; set; }
    }
}