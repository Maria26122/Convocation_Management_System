namespace Convocation.Entities
{
    public class QrPass
    {
        public int QrPassId { get; set; }

        public int RegistrationId { get; set; }
        public Registration Registration { get; set; }

        public string QrCodeText { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UsedAt { get; set; }

        public string QrImagePath { get; set; }
    }
}