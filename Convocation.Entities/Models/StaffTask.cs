using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convocation.Entities
{
    public class StaffTask
    {
        public int StaffTaskId { get; set; }

        public int DistributionTaskId { get; set; }
        public DistributionTask? DistributionTask { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }
    }
}
