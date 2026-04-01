using Convocation_Management_System.Web.UI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(200)]
        public required string EventTitle { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(200)]
        public required string Venue { get; set; }

        [Required]
        public DateTime RegistrationStartDate { get; set; }

        [Required]
        public DateTime RegistrationEndDate { get; set; }

        [Required]
        public decimal BaseFee { get; set; }

        [Required]
        public decimal GuestFee { get; set; }

        [Required]
        public int MaxGuestAllowed { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}