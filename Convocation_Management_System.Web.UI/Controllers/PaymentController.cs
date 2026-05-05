using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ConvocationDbContext _context;

        public PaymentController(ConvocationDbContext context)
        {
            _context = context;
        }

        private string CurrentRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();
        }

        private bool IsAdmin()
        {
            return CurrentRole() == "admin";
        }

        private bool IsStaff()
        {
            return CurrentRole() == "staff" || CurrentRole() == "eventmanager";
        }

        private bool IsStudentLoggedIn()
        {
            var role = CurrentRole();
            return role == "student" || role == "participant";
        }

        private int? GetLoggedInUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            if (!int.TryParse(userId, out int parsedId))
                return null;

            return parsedId;
        }

        private async Task<Participant?> GetLoggedInParticipantAsync()
        {
            var userId = GetLoggedInUserId();
            if (userId == null)
                return null;

            return await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);
        }

        private async Task LoadRegistrationDropdownAsync(object? selectedId = null)
        {
            var registrations = await _context.Registration
                .Include(r => r.Participant)
                    .ThenInclude(p => p.UserAccount)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegistrationId)
                .Select(r => new
                {
                    r.RegistrationId,
                    DisplayText = "Reg#" + r.RegistrationId
                                  + " - "
                                  + (r.Participant != null && r.Participant.UserAccount != null
                                        ? r.Participant.UserAccount.FullName
                                        : "Unknown Participant")
                                  + " - "
                                  + (r.Event != null ? r.Event.EventTitle : "No Event")
                })
                .ToListAsync();

            ViewBag.RegistrationId = new SelectList(registrations, "RegistrationId", "DisplayText", selectedId);
        }

        private async Task GenerateQrIfNotExistsAsync(int registrationId)
        {
            var existingQr = await _context.QrPass
                .FirstOrDefaultAsync(q => q.RegistrationId == registrationId);

            if (existingQr != null)
                return;

            var qrPass = new QrPass
            {
                RegistrationId = registrationId,
                QrCodeText = "CONVO-" + registrationId + "-" + Guid.NewGuid().ToString("N"),
                QrImagePath = null,
                IssuedAt = DateTime.Now,
                IsUsed = false
            };

            _context.QrPass.Add(qrPass);
        }

        // ==============================
        // ADMIN LIST
        // ==============================
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var payments = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.PaymentId)
                .ToListAsync();

            return View(payments);
        }

        // ==============================
        // ADMIN DETAILS
        // ==============================
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var payment = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(p => p.PaymentId == id.Value);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        // ==============================
        // ADMIN CREATE
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            await LoadRegistrationDropdownAsync();

            var model = new Payment
            {
                RegistrationId = 0,
                Registration = null!, // temporary for view loading
                PaymentStatus = "Pending",
                PaymentDate = DateTime.Now,
                PaymentMethod = "",
                TransactionId = null,
                PaidAmount = 0,
                SessionKey = null
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Registration");

            var registration = await _context.Registration
                .FirstOrDefaultAsync(r => r.RegistrationId == payment.RegistrationId);

            if (registration == null)
            {
                ModelState.AddModelError("RegistrationId", "Invalid registration.");
            }

            if (!ModelState.IsValid)
            {
                payment.Registration = registration ?? null!;
                await LoadRegistrationDropdownAsync(payment.RegistrationId);
                return View(payment);
            }

            payment.Registration = registration!;

            _context.Payment.Add(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // ADMIN EDIT
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var payment = await _context.Payment.FindAsync(id.Value);
            if (payment == null)
                return NotFound();

            await LoadRegistrationDropdownAsync(payment.RegistrationId);
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id != payment.PaymentId)
                return NotFound();

            ModelState.Remove("Registration");

            var existing = await _context.Payment.FindAsync(id);
            if (existing == null)
                return NotFound();

            bool validRegistration = await _context.Registration
                .AnyAsync(r => r.RegistrationId == payment.RegistrationId);

            if (!validRegistration)
                ModelState.AddModelError("RegistrationId", "Invalid registration.");

            if (!ModelState.IsValid)
            {
                await LoadRegistrationDropdownAsync(payment.RegistrationId);
                return View(payment);
            }

            existing.RegistrationId = payment.RegistrationId;
            existing.PaymentMethod = payment.PaymentMethod;
            existing.TransactionId = payment.TransactionId;
            existing.PaidAmount = payment.PaidAmount;
            existing.PaymentStatus = payment.PaymentStatus;
            existing.PaymentDate = payment.PaymentDate;
            existing.SessionKey = payment.SessionKey;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // ADMIN DELETE
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var payment = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(p => p.PaymentId == id.Value);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var payment = await _context.Payment.FindAsync(id);
            if (payment == null)
                return NotFound();

            _context.Payment.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // STUDENT PAYMENT FLOW
        // ==============================
        public async Task<IActionResult> PayNow(int? registrationId = null)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var participant = await GetLoggedInParticipantAsync();

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            Registration? registration;

            if (registrationId.HasValue)
            {
                registration = await _context.Registration
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r =>
                        r.RegistrationId == registrationId.Value &&
                        r.ParticipantId == participant.ParticipantId);
            }
            else
            {
                registration = await _context.Registration
                    .Include(r => r.Event)
                    .Where(r => r.ParticipantId == participant.ParticipantId)
                    .OrderByDescending(r => r.RegistrationDate)
                    .FirstOrDefaultAsync();
            }

            if (registration == null)
            {
                TempData["Error"] = "No registration found for payment.";
                return RedirectToAction("MyRegistration", "Participant");
            }

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            if (payment == null)
            {
                payment = new Payment
                {
                    RegistrationId = registration.RegistrationId,
                    Registration = registration,
                    PaidAmount = registration.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentMethod = "Manual/Sandbox",
                    TransactionId = null,
                    PaymentDate = null,
                    SessionKey = Guid.NewGuid().ToString("N")
                };

                _context.Payment.Add(payment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Checkout), new { registrationId = registration.RegistrationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPayment(int registrationId)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var participant = await GetLoggedInParticipantAsync();
            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registration
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r =>
                    r.RegistrationId == registrationId &&
                    r.ParticipantId == participant.ParticipantId);

            if (registration == null)
            {
                TempData["Error"] = "Invalid registration for payment.";
                return RedirectToAction("MyRegistration", "Participant");
            }

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            if (payment == null)
            {
                payment = new Payment
                {
                    Registration = registration,
                    RegistrationId = registration.RegistrationId,
                    PaidAmount = registration.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentMethod = "SSLCommerz",
                    TransactionId = null,
                    PaymentDate = null,
                    SessionKey = null
                };

                _context.Payment.Add(payment);
            }
            else
            {
                payment.PaidAmount = registration.TotalAmount;
                payment.PaymentMethod = "SSLCommerz";

                if (string.IsNullOrWhiteSpace(payment.PaymentStatus))
                    payment.PaymentStatus = "Pending";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Checkout), new { registrationId = registration.RegistrationId });
        }

        public async Task<IActionResult> Checkout(int registrationId)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var participant = await GetLoggedInParticipantAsync();

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                .FirstOrDefaultAsync(p =>
                    p.RegistrationId == registrationId &&
                    p.Registration != null &&
                    p.Registration.ParticipantId == participant.ParticipantId);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction("MyRegistration", "Participant");
            }

            return View(payment);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int registrationId)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var participant = await GetLoggedInParticipantAsync();

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payment
                .Include(p => p.Registration)
                .FirstOrDefaultAsync(p =>
                    p.RegistrationId == registrationId &&
                    p.Registration != null &&
                    p.Registration.ParticipantId == participant.ParticipantId);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction("MyPayment", "Participant");
            }

            payment.PaymentStatus = "Paid";
            payment.PaymentMethod = "Manual/Sandbox";
            payment.PaymentDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(payment.TransactionId))
                payment.TransactionId = "TXN-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            if (payment.Registration != null)
            {
                payment.Registration.RegistrationStatus = "Paid";
                await GenerateQrIfNotExistsAsync(payment.RegistrationId);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment completed successfully. QR pass generated.";
            return RedirectToAction("MyQrPass", "Participant");
        }

        // ==============================
        // ADMIN PAYMENT STATUS UPDATE
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentStatus(int paymentId, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var payment = await _context.Payment.FindAsync(paymentId);
            if (payment == null)
                return NotFound();

            payment.PaymentStatus = status;

            if (status == "Paid" && payment.PaymentDate == null)
                payment.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment status updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
        
    
