using Convocation.DataAccess;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ConvocationDbContext _context;

        public ParticipantController(ConvocationDbContext context)
        {
            _context = context;
        }

        private bool IsStudentLoggedIn()
        {
            var role = HttpContext.Session.GetString("Role");
            return !string.IsNullOrEmpty(role) &&
                   (role.Trim().ToLower() == "student" || role.Trim().ToLower() == "participant");
        }

        private int? GetLoggedInUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return null;

            return int.Parse(userId);
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participants
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.ParticipantId == participant.ParticipantId);

            var payment = registration == null
                ? null
                : await _context.Payments.FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            var qrPass = registration == null
                ? null
                : await _context.QrPasses.FirstOrDefaultAsync(q => q.RegistrationId == registration.RegistrationId);

            var guests = registration == null
                ? new List<Convocation.Entities.Guest>()
                : await _context.Guests.Where(g => g.RegistrationId == registration.RegistrationId).ToListAsync();

            int progress = 20;

            if (!string.IsNullOrWhiteSpace(participant.StudentId) &&
                !string.IsNullOrWhiteSpace(participant.Department) &&
                !string.IsNullOrWhiteSpace(participant.Program) &&
                !string.IsNullOrWhiteSpace(participant.Session))
            {
                progress += 20;
            }

            if (registration != null)
                progress += 20;

            if (payment != null && payment.PaymentStatus == "Paid")
                progress += 20;

            if (qrPass != null)
                progress += 20;

            var model = new ParticipantDashboardViewModel
            {
                FullName = participant.UserAccount?.FullName ?? "",
                Email = participant.UserAccount?.Email ?? "",
                Phone = participant.UserAccount?.Phone ?? "",

                ParticipantId = participant.ParticipantId,
                StudentId = participant.StudentId,
                Department = participant.Department,
                Program = participant.Program,
                Session = participant.Session,
                IsEligible = participant.IsEligible,

                RegistrationId = registration?.RegistrationId,
                RegistrationStatus = registration?.RegistrationStatus ?? "Not Registered",
                RegistrationDate = registration?.RegistrationDate,
                GuestCount = guests.Count,

                EventTitle = registration?.Event?.EventTitle ?? "No Event Assigned",
                EventDate = registration?.Event?.EventDate,
                Venue = registration?.Event?.Venue ?? "TBA",
                MaxGuestAllowed = registration?.Event?.MaxGuestAllowed ?? 0,

                PaymentStatus = payment?.PaymentStatus ?? "Pending",
                PaidAmount = payment?.PaidAmount ?? 0,
                PaymentDate = payment?.PaymentDate,
                TransactionId = payment?.TransactionId,

                HasQrPass = qrPass != null,
                IsQrUsed = qrPass?.IsUsed ?? false,
                QrCodeText = qrPass?.QrCodeText ?? "",

                CompletionPercentage = progress
            };

            return View(model);
        }

        public async Task<IActionResult> MyProfile()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participants
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            return View(participant);
        }

        public async Task<IActionResult> MyRegistration()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.ParticipantId == participant.ParticipantId);

            return View(registration);
        }

        public async Task<IActionResult> MyPayment()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.ParticipantId == participant.ParticipantId);

            if (registration == null)
                return View(null);

            var payment = await _context.Payments
                .Include(p => p.Registration)
                .FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            return View(payment);
        }

        public IActionResult MyQrPass()
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdString);

            var participant = _context.Participants
                .FirstOrDefault(p => p.UserAccountId == userId);

            if (participant == null)
                return View(null);

            var qr = _context.QrPasses
                .Include(q => q.Registration)
                .FirstOrDefault(q => q.Registration.ParticipantId == participant.ParticipantId);

            return View(qr);
        }

        public async Task<IActionResult> MyGuests()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.ParticipantId == participant.ParticipantId);

            if (registration == null)
                return View(new List<Convocation.Entities.Guest>());

            var guests = await _context.Guests
                .Where(g => g.RegistrationId == registration.RegistrationId)
                .ToListAsync();

            return View(guests);
        }
    }
}