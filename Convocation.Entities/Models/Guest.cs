using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class Guest
    {
        [Key]
        public int GuestId { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [Required]
        [StringLength(120)]
        public string GuestName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Relation { get; set; }

        [ForeignKey("RegistrationId")]
        public virtual Registration? Registration { get; set; }
    }
}