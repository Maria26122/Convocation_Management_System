using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "staff,eventmanager,admin")]
    public class DistributionScanController : Controller
    {
        private readonly ConvocationDbContext _context;

        public DistributionScanController(ConvocationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Scan()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Verify(string qrText, int distributionTaskId)
        {
            var qr = await _context.QrPass
                .Include(x => x.Registration)
                    .ThenInclude(r => r.Participant)
                .FirstOrDefaultAsync(x => x.QrCodeText == qrText);

            if (qr == null)
            {
                return Json(new { success = false, message = "Invalid QR" });
            }

            if (qr.IsUsed)
            {
                return Json(new { success = false, message = "QR already used" });
            }

            var log = new DistributionLog
            {
                RegistrationId = qr.RegistrationId,
                ParticipantId = qr.Registration.ParticipantId,
                EventId = qr.Registration.EventId,
                UserAccountId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                DistributionTaskId = distributionTaskId,
                ActionType = "QR Verification",
                ActionDate = DateTime.Now,
                IsDelivered = true,
                IsQrVerified = true
            };

            qr.IsUsed = true;

            _context.DistributionLog.Add(log);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "QR verified successfully",
                student = qr.Registration.Participant.StudentId
            });
        }
    }
}
