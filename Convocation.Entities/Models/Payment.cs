using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

       [Required]
        public int RegistrationId { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; } = null;  

        [StringLength(120)]
        public string? TransactionId { get; set; }

        [Required]
        public decimal PaidAmount { get; set; }

        [Required]
        [StringLength(30)]
        public required string PaymentStatus { get; set; }

        public DateTime? PaymentDate { get; set; }

        [ForeignKey("RegistrationId")]
        public virtual required Registration Registration { get; set; }
        public string? SessionKey { get; set; }
    }
}