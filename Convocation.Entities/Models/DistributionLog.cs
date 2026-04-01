using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class DistributionLog
    {
        [Key]
        public int DistributionLogId { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [Required]
        public int UserAccountId { get; set; }

        [Required]
        [StringLength(30)]
        public string ActionType { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Note { get; set; }

        [ForeignKey("RegistrationId")]
        public virtual Registration? Registration { get; set; }

        [ForeignKey("UserAccountId")]
        public virtual UserAccount? UserAccount { get; set; }
    }
}