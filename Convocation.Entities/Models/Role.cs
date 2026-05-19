using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public ICollection<UserAccount> UserAccounts { get; set; }
            = new List<UserAccount>();

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();
    }
}