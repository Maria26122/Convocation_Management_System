using System;
using System.ComponentModel.DataAnnotations;

namespace Convocation.Entities
{
    public class OperationActivityLog
    {
        [Key]
        public int OperationActivityLogId { get; set; }

        public string ActivityType { get; set; } = string.Empty;
        // QR_SCAN / DELIVERY / TASK_UPDATE

        public string Message { get; set; } = string.Empty;

        public DateTime Time { get; set; } = DateTime.Now;

        public int? UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }
    }
}