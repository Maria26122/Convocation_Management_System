using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class UserPermission
    {
        [Key]
        public int UserPermissionId { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }

        public int PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}