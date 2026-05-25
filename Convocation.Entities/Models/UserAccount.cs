using Convocation.Entities.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class UserAccount
    {
        [Key]
        public int UserAccountId { get; set; }

        public string? FullName { get; set; }
        public string? NickName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Participant? Participant { get; set; }

        public ICollection<DistributionLog> DistributionLog { get; set; }
            = new List<DistributionLog>();

        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
    }
}