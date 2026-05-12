using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities.Models
{
    public class DistributionItem
    {
        [Key]
        public int DistributionItemId { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ItemType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool RequiresQrValidation { get; set; } = true;
    }
}
