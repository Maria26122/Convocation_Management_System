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

        [ForeignKey("RegistrationId")]
        public Registration? Registration { get; set; }

        public int? UserAccountId { get; set; }

        [ForeignKey("UserAccountId")]
        public UserAccount? UserAccount { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = "";

        public DateTime ActionDate { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Note { get; set; }

        [StringLength(200)]
        public string? Remarks { get; set; }

        public int? FoodMenuId { get; set; }

        [ForeignKey(nameof(FoodMenuId))]
        public FoodMenu? FoodMenu { get; set; }

        [StringLength(100)]
        public string DistributionStatus { get; set; } = "Completed";

        [StringLength(100)]
        public string? VerificationMethod { get; set; }

        [StringLength(300)]
        public string? DistributionQrToken { get; set; }
    }
}