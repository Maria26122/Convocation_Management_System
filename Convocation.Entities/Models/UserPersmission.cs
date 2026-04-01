using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class UserPermission
    {
        [Key]
        public int UserPermissionId { get; set; }

        [Required]
        public int UserAccountId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        [ForeignKey("UserAccountId")]
        public virtual UserAccount? UserAccount { get; set; }

        [ForeignKey("PermissionId")]
        public virtual Permission? Permission { get; set; }
    }
}