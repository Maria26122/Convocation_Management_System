using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class FoodMenu
    {
        [Key]
        public int FoodMenuId { get; set; }

        [Required]
        [StringLength(100)]
        public string MenuName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string MenuItems { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}