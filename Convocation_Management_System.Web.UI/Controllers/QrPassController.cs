using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class QrPassController : Controller
    {
        private readonly ConvocationDbContext _context;
        private readonly QrGeneratorService _qrService;

        public QrPassController(
            ConvocationDbContext context,
            QrGeneratorService qrService)
        {
            _context = context;
            _qrService = qrService;
        }

        // =====================
        // INDEX
        // =====================
        public async Task<IActionResult> Index()
        {
            var list = await _context.QrPass
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Event)
                .OrderByDescending(q => q.QrPassId)
                .ToListAsync();

            return View(list);
        }

        // =====================
        // CREATE (GET)
        // =====================
        public IActionResult Create()
        {
            ViewBag.RegistrationId = new SelectList(
                _context.Registration
                    .Include(r => r.Participant)
                    .Select(r => new
                    {
                        r.RegistrationId,
                        Text = r.RegistrationId + " - " + r.Participant.StudentId
                    }),
                "RegistrationId",
                "Text"
            );

            return View();
        }

        // =====================
        // CREATE (POST)
        // =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QrPass model)
        {
            var registration = await _context.Registration
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.RegistrationId == model.RegistrationId);

            if (registration == null)
            {
                ModelState.AddModelError("", "Invalid registration");
            }

            if (registration?.Payment == null ||
                registration.Payment.PaymentStatus != "Paid")
            {
                ModelState.AddModelError("", "Payment not completed");
            }

            bool exists = await _context.QrPass
                .AnyAsync(q => q.RegistrationId == model.RegistrationId);

            if (exists)
            {
                ModelState.AddModelError("", "QR already exists");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string qrText =
                $"CONV-{model.RegistrationId}-{Guid.NewGuid().ToString("N")[..8]}";

            model.QrCodeText = qrText;
            model.CreatedAt = DateTime.Now;
            model.IsUsed = false;

            model.QrImagePath = _qrService.GenerateQr(qrText);

            _context.QrPass.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =====================
        // SCAN PAGE
        // =====================
        public IActionResult Scan()
        {
            return View();
        }
    }
}