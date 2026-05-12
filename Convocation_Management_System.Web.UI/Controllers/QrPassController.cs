using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class QrPassController : BaseController
    {
        private readonly ConvocationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public QrPassController(ConvocationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var qrPasses = await _context.QrPass
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Event)
                .OrderByDescending(q => q.QrPassId)
                .ToListAsync();

            return View(qrPasses ?? new List<QrPass>());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var qrPass = await _context.QrPass
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(q => q.QrPassId == id);

            if (qrPass == null) return NotFound();

            return View(qrPass);
        }

        public IActionResult Create()
        {
            LoadRegistrationDropdown();
            return View(new QrPass
            {
                IsUsed = false,
                IssuedAt = DateTime.Now,
                QrCodeText = string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QrPass qrPass)
        {
            ModelState.Remove("QrCodeText");

            bool alreadyExists = await _context.QrPass
                .AnyAsync(q => q.RegistrationId == qrPass.RegistrationId);

            if (alreadyExists)
            {
                ModelState.AddModelError("", "QR pass already exists for this registration.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    qrPass.IssuedAt = DateTime.Now;
                    qrPass.IsUsed = false;
                    qrPass.QrCodeText = $"REG-{qrPass.RegistrationId}-PASS-{Guid.NewGuid().ToString().Substring(0, 8)}";
                    qrPass.QrImagePath = GenerateQrImage(qrPass.QrCodeText);

                    _context.QrPass.Add(qrPass);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "QR Pass generated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                }
            }

            LoadRegistrationDropdown(qrPass.RegistrationId);
            return View(qrPass);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var qrPass = await _context.QrPass.FindAsync(id);
            if (qrPass == null) return NotFound();

            LoadRegistrationDropdown(qrPass.RegistrationId);
            return View(qrPass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QrPass qrPass)
        {
            if (id != qrPass.QrPassId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(qrPass);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.QrPass.Any(e => e.QrPassId == qrPass.QrPassId))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            LoadRegistrationDropdown(qrPass.RegistrationId);
            return View(qrPass);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var qrPass = await _context.QrPass
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(q => q.QrPassId == id);

            if (qrPass == null) return NotFound();

            return View(qrPass);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var qrPass = await _context.QrPass.FindAsync(id);
            if (qrPass != null)
            {
                _context.QrPass.Remove(qrPass);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: QrPass/Verify
        public IActionResult Verify()
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();

            if (role != "admin" && role != "staff" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: QrPass/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return View();
            }

            var qr = await _context.QrPass
                .Include(x => x.Registration)
                .FirstOrDefaultAsync(x => x.QrCodeText == code);

            if (qr == null)
            {
                TempData["ErrorMessage"] = "Invalid QR Code";
                return View();
            }

            return View("VerifyResult", qr.Registration);
        }

        // POST: QrPass/ConfirmEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEntry(int qrPassId)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();

            if (role != "admin" && role != "staff" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            var userIdText = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdText, out int userId))
                return RedirectToAction("Login", "Account");

            var qrPass = await _context.QrPass
                .Include(q => q.Registration)
                .FirstOrDefaultAsync(q => q.QrPassId == qrPassId);

            if (qrPass == null || qrPass.Registration == null)
                return NotFound();

            if (qrPass.Registration.RegistrationStatus != "Paid")
            {
                TempData["ErrorMessage"] = "Payment is not completed.";
                return RedirectToAction(nameof(Verify));
            }

            if (qrPass.IsUsed)
            {
                TempData["ErrorMessage"] = "Entry already confirmed.";
                return RedirectToAction(nameof(Verify));
            }

            qrPass.IsUsed = true;

            var log = new DistributionLog
            {
                RegistrationId = qrPass.RegistrationId,
                UserAccountId = userId,
                ActionType = "Entry Confirmation",
                ActionDate = DateTime.Now,
                Note = "QR Check-In",
                Remarks = "Entry confirmed by QR scanner."
            };

            _context.QrPass.Update(qrPass);
            _context.DistributionLog.Add(log);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Entry confirmed successfully.";
            return RedirectToAction(nameof(Verify));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmItem(int qrPassId, string itemType)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();

            if (role != "admin" && role != "staff" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            var userIdText = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdText, out int userId))
                return RedirectToAction("Login", "Account");

            var allowedItems = new[] { "Food", "Kit", "Certificate", "Gown" };

            if (!allowedItems.Contains(itemType))
            {
                TempData["ErrorMessage"] = "Invalid distribution item.";
                return RedirectToAction(nameof(Verify));
            }

            var qrPass = await _context.QrPass
                .Include(q => q.Registration)
                .FirstOrDefaultAsync(q => q.QrPassId == qrPassId);

            if (qrPass == null || qrPass.Registration == null)
                return NotFound();

            if (qrPass.Registration.RegistrationStatus != "Paid")
            {
                TempData["ErrorMessage"] = "Payment is not completed.";
                return RedirectToAction(nameof(Verify));
            }

            bool alreadyDistributed = await _context.DistributionLog
                .AnyAsync(d => d.RegistrationId == qrPass.RegistrationId &&
                               d.ActionType == itemType);

            if (alreadyDistributed)
            {
                TempData["ErrorMessage"] = itemType + " already distributed.";
                return RedirectToAction(nameof(Verify));
            }

            var log = new DistributionLog
            {
                RegistrationId = qrPass.RegistrationId,
                UserAccountId = userId,
                ActionType = itemType,
                ActionDate = DateTime.Now,
                Note = itemType + " Distribution",
                Remarks = itemType + " distributed successfully."
            };

            if (itemType == "Food")
            {
                var menu = await _context.FoodMenu.FirstOrDefaultAsync(x => x.IsActive);

                log.FoodMenuId = menu?.FoodMenuId;

                log.Remarks = menu != null
                    ? "Food menu issued: " + menu.MenuName
                    : "Food distributed.";
            }

            var distributionQr = await _context.StudentDistributionQr
                .FirstOrDefaultAsync(x => x.QrToken == qrPass.QrCodeText
                                       && x.DistributionType == itemType);

            if (distributionQr == null)
            {
                TempData["ErrorMessage"] = "Invalid distribution QR.";
                return RedirectToAction(nameof(Verify));
            }

            if (distributionQr.IsUsed)
            {
                TempData["ErrorMessage"] = itemType + " already collected.";
                return RedirectToAction(nameof(Verify));
            }

            _context.DistributionLog.Add(log);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = itemType + " distributed successfully.";
            return RedirectToAction(nameof(Verify));
        }
        private void LoadRegistrationDropdown(object? selectedRegistration = null)
        {
            var registrations = _context.Registration
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .AsEnumerable()
                .Select(r => new
                {
                    RegistrationId = r.RegistrationId,
                    DisplayText = $"Reg #{r.RegistrationId} - {r.Participant?.StudentId ?? "No Student"} - {r.Event?.EventTitle ?? "No Event"}"
                })
                .ToList();

            ViewBag.RegistrationId = new SelectList(registrations, "RegistrationId", "DisplayText", selectedRegistration);
        }

        private string GenerateQrImage(string qrText)
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string folderPath = Path.Combine(webRoot, "qrcodes");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = $"qr_{Guid.NewGuid()}.png";
            string fullPath = Path.Combine(folderPath, fileName);

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
            {
                qrCodeImage.Save(fullPath, ImageFormat.Png);
            }

            return "/qrcodes/" + fileName;
        }

        private async Task GenerateDistributionQrs(int registrationId)
        {
            string[] distributionTypes =
            {
        "Food",
        "Kit",
        "Gown",
        "Certificate"
    };

            foreach (var type in distributionTypes)
            {
                bool exists = await _context.StudentDistributionQr
                    .AnyAsync(x => x.RegistrationId == registrationId
                                && x.DistributionType == type);

                if (exists)
                    continue;

                var token = $"DIST-{registrationId}-{type}-{Guid.NewGuid().ToString()[..8]}";

                var qr = new StudentDistributionQr
                {
                    RegistrationId = registrationId,
                    DistributionType = type,
                    QrToken = token,
                    QrImagePath = GenerateQrImage(token),
                    CreatedAt = DateTime.Now
                };

                _context.StudentDistributionQr.Add(qr);
            }

            await _context.SaveChangesAsync();
        }
    }
}