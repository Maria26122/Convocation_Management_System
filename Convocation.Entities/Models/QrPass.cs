using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class QrPass
    {
        public int QrPassId { get; set; }
        public int RegistrationId { get; set; }

        [Column("QrCode")]
        public string QrCodeText { get; set; } = string.Empty;

        public string? QrImagePath { get; set; }
        public DateTime IssuedAt { get; set; }
        public bool IsUsed { get; set; }

        public virtual Registration? Registration { get; set; }
    }
}