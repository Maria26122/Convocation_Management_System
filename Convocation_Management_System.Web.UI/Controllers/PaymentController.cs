using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
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

        // ==============================
        // COMMON HELPERS
        // ==============================
        private string CurrentRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();
        }

        private bool IsAdmin()
        {
            return CurrentRole() == "admin";
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

            return await _context.Participants
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);
        }

        // ==============================
        // ADMIN PAYMENT LIST
        // ==============================
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var payments = await _context.Payments
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
        // STUDENT: OPEN OWN PAYMENT PAGE
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
                registration = await _context.Registrations
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r =>
                        r.RegistrationId == registrationId.Value &&
                        r.ParticipantId == participant.ParticipantId);
            }
            else
            {
                registration = await _context.Registrations
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

            var payment = await _context.Payments
                .Include(p => p.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            if (payment == null)
            {
                payment = new Payment
                {
                    RegistrationId = registration.RegistrationId,
                    Registration = registration,
                    PaidAmount = registration.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentMethod = "SSLCommerz",
                    TransactionId = null,
                    PaymentDate = null,
                    SessionKey = null
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
            }

            return View(payment);
        }

        // ==============================
        // STUDENT: START PAYMENT
        // ==============================
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

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r =>
                    r.RegistrationId == registrationId &&
                    r.ParticipantId == participant.ParticipantId);

            if (registration == null)
            {
                TempData["Error"] = "Invalid registration for payment.";
                return RedirectToAction("MyRegistration", "Participant");
            }

            var payment = await _context.Payments
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

                _context.Payments.Add(payment);
            }
            else
            {
                payment.PaidAmount = registration.TotalAmount;
                payment.PaymentMethod = "SSLCommerz";

                if (string.IsNullOrWhiteSpace(payment.PaymentStatus))
                    payment.PaymentStatus = "Pending";
            }

            await _context.SaveChangesAsync();

            // TEMPORARY FLOW:
            // Later replace this with SSLCommerz Session creation and redirect URL
            return RedirectToAction(nameof(Checkout), new { registrationId = registration.RegistrationId });
        }

        // ==============================
        // STUDENT: TEMP CHECKOUT PAGE
        // ==============================
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

            var payment = await _context.Payments
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(p =>
                    p.RegistrationId == registrationId &&
                    p.Registration != null &&
                    p.Registration.ParticipantId == participant.ParticipantId);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction("MyPayment", "Participant");
            }

            return View(payment);
        }

        // ==============================
        // TEMP SUCCESS FOR DEMO / TESTING
        // Replace with SSLCommerz callback later
        // ==============================
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

            var payment = await _context.Payments
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
            payment.PaymentDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(payment.TransactionId))
            {
                payment.TransactionId = "TXN-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment completed successfully.";
            return RedirectToAction("MyPayment", "Participant");
        }

        // ==============================
        // OPTIONAL ADMIN STATUS UPDATE
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int paymentId, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var payment = await _context.Payments.FindAsync(paymentId);
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