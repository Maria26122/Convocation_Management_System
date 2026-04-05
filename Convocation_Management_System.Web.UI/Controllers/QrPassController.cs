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
    public class QrPassController : Controller
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
            var qrPasses = await _context.QrPasses
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

            var qrPass = await _context.QrPasses
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
            return View(new QrPass { IsUsed = false, IssuedAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QrPass qrPass)
        {
            ModelState.Remove("QrCodeText");

            bool alreadyExists = await _context.QrPasses
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

                    _context.QrPasses.Add(qrPass);
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

            var qrPass = await _context.QrPasses.FindAsync(id);
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
                    if (!_context.QrPasses.Any(e => e.QrPassId == qrPass.QrPassId))
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

            var qrPass = await _context.QrPasses
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
            var qrPass = await _context.QrPasses.FindAsync(id);
            if (qrPass != null)
            {
                _context.QrPasses.Remove(qrPass);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: QrPass/Verify
        public IActionResult Verify()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin" && role != "Staff")
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // POST: QrPass/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> Verify(string qrCodeText)
        {
            if (string.IsNullOrWhiteSpace(qrCodeText))
            {
                ModelState.AddModelError("", "Please enter a QR code.");
                return View();
            }

            var qrPass = await _context.QrPasses
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(q => q.QrCodeText.Trim() == qrCodeText.Trim());

            if (qrPass == null)
            {
                ModelState.AddModelError("", "QR code not found.");
                return View();
            }

            return View("VerifyResult", qrPass);
        }

        // POST: QrPass/ConfirmEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> ConfirmEntry(int qrPassId)
        {
            var qrPass = await _context.QrPasses
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(q => q.QrPassId == qrPassId);

            if (qrPass == null)
            {
                return NotFound();
            }

            if (qrPass.IsUsed)
            {
                TempData["ErrorMessage"] = "This QR pass has already been used.";
                return RedirectToAction(nameof(Verify));
            }

            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Account");
            }

            if (!int.TryParse(userIdString, out var userId))
            {
                TempData["ErrorMessage"] = "Invalid session. Please login again.";
                return RedirectToAction("Login", "Account");
            }

            qrPass.IsUsed = true;

            var log = new DistributionLog
            {
                RegistrationId = qrPass.RegistrationId,
                UserAccountId = userId,
                ActionType = "Entry Confirmed",
                ActionDate = DateTime.Now,
                Note = "Verified by QR check-in"
            };

            // Ensure the modified qrPass is tracked and save both changes atomically
            _context.Update(qrPass);
            _context.DistributionLogs.Add(log);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while verifying the entry. " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction(nameof(Verify));
            }

            TempData["SuccessMessage"] = "Entry verified successfully.";
            return RedirectToAction(nameof(Verify));
        }
        private void LoadRegistrationDropdown(object? selectedRegistration = null)
        {
            var registrations = _context.Registrations
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
            string folderPath = Path.Combine(_environment.WebRootPath, "qrcodes");

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
    }
}