using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }

        [Required]
        public string PermissionName { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();

        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
    }
}