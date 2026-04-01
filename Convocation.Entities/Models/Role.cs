using Convocation.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public virtual ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}