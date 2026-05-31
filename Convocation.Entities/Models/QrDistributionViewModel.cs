using System;
using System.Collections.Generic;
using System.Text;

namespace Convocation_Management_System.Web.UI.Models
{
    public class QrDistributionViewModel
    {
        public string QrCodeText { get; set; } = string.Empty;

        public int DistributionTaskId { get; set; }

        public string? Message { get; set; }

        public bool IsSuccess { get; set; }

        public string? StudentName { get; set; }

        public string? StudentId { get; set; }
    }
}