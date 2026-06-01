using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "staff,eventmanager,admin")]
    public class QrScanController : Controller
    {
        private readonly ConvocationDbContext _context;

        public QrScanController(ConvocationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Scan(int taskId = 0)
        {
            var model = new QrDistributionViewModel
            {
                DistributionTaskId = taskId
            };

            return View(model);
        }



        // =========================
        // VERIFY QR
        // =========================
   
        [HttpPost]
        public async Task<IActionResult> Verify(QrDistributionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.QrCodeText))
            {
                model.Message = "QR is empty";
                return View("Scan", model);
            }

            var qr = await _context.QrPass
                .Include(x => x.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(x => x.QrCodeText == model.QrCodeText);

            if (qr == null)
            {
                model.IsSuccess = false;
                model.Message = "Invalid QR Code";
                return View("Scan", model);
            }

            if (qr.IsUsed)
            {
                model.IsSuccess = false;
                model.Message = "QR already used";
                return View("Scan", model);
            }

            var userId = Convert.ToInt32(HttpContext.Session.GetString("UserId") ?? "0");

            qr.IsUsed = true;
            qr.UsedAt = DateTime.Now;

            _context.DistributionLog.Add(new DistributionLog
            {
                RegistrationId = qr.RegistrationId,
                ParticipantId = qr.Registration.ParticipantId,
                EventId = qr.Registration.EventId,
                UserAccountId = userId,
                DistributionTaskId = model.DistributionTaskId,
                ActionType = "QR SCAN",
                ActionDate = DateTime.Now,
                IsDelivered = true,
                IsQrVerified = true
            });

            await _context.SaveChangesAsync();

            model.IsSuccess = true;
            model.Message = "QR Verified Successfully";
            model.StudentId = qr.Registration.Participant.StudentId;
            model.StudentName = qr.Registration.Participant.UserAccount.FullName;

            return View("Scan", model);
        }
    }
}