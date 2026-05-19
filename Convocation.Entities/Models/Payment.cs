using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int RegistrationId { get; set; }
        public Registration? Registration { get; set; }

        public decimal PaidAmount { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "SSLCommerz";

        [StringLength(120)]
        public string? TransactionId { get; set; }

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? VerifiedAt { get; set; }
    }
}