using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class StudentDistributionQr
    {
        [Key]
        public int StudentDistributionQrId { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [ForeignKey(nameof(RegistrationId))]
        public Registration? Registration { get; set; }

        [Required]
        [StringLength(50)]
        public string DistributionType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string QrToken { get; set; } = string.Empty;

        [StringLength(300)]
        public string? QrImagePath { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}