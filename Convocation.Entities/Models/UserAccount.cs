using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class UserAccount
    {
        [Key]
        public int UserAccountId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }

       [Required]
        public string PasswordHash { get; set; } = "";

        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // OTP for 2FA
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiryTime { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = true;

        // Navigation properties
        public virtual Role? Role { get; set; }
        public virtual Participant? Participant { get; set; }

        public virtual ICollection<UserPermission>? UserPermissions { get; set; }
    }
}