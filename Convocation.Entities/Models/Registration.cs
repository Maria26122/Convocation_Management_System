using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class Registration
    {
        [Key]
        public int RegistrationId { get; set; }

        [Required]
        public int ParticipantId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Range(0, 10)]
        public int GuestCount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string RegistrationStatus { get; set; } = "Pending";

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [ForeignKey("ParticipantId")]
        public Participant? Participant { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        public virtual ICollection<DistributionLog> DistributionLogs { get; set; } = new List<DistributionLog>();
    }
}