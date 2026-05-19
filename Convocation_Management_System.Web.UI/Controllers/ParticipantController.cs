using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ConvocationDbContext _context;

        public ParticipantController(ConvocationDbContext context)
        {
            _context = context;
        }

        // =========================
        // HELPERS
        // =========================
        private int? GetUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return int.TryParse(userId, out int id) ? id : null;
        }

        private string GetRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").ToLower();
        }

        private bool IsParticipant()
        {
            var role = GetRole();
            return role == "participant" || role == "student";
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("Role")?.ToLower();

            if (role != "participant" && role != "student")
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registration
                .Include(r => r.Event)
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            var payment = registration == null
                ? null
                : await _context.Payment.FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            var qrPass = registration == null
                ? null
                : await _context.QrPass.FirstOrDefaultAsync(q => q.RegistrationId == registration.RegistrationId);

            var guests = registration == null
                ? new List<Guest>()
                : await _context.Guest.Where(g => g.RegistrationId == registration.RegistrationId).ToListAsync();

            int progress = 20;

            if (!string.IsNullOrWhiteSpace(participant.StudentId) &&
                !string.IsNullOrWhiteSpace(participant.Department) &&
                !string.IsNullOrWhiteSpace(participant.Program) &&
                !string.IsNullOrWhiteSpace(participant.Session))
            {
                progress += 20;
            }

            if (registration != null) progress += 20;
            if (payment != null && payment.PaymentStatus == "Paid") progress += 20;
            if (qrPass != null) progress += 20;

            return View(new ParticipantDashboardViewModel
            {
                FullName = participant.UserAccount?.FullName,
                Email = participant.UserAccount?.Email,
                Phone = participant.UserAccount?.Phone,

                ParticipantId = participant.ParticipantId,
                StudentId = participant.StudentId,
                Department = participant.Department,
                Program = participant.Program,
                Session = participant.Session,

                RegistrationId = registration?.RegistrationId,
                RegistrationStatus = registration?.RegistrationStatus ?? "Not Registered",

                GuestCount = guests.Count,

                PaymentStatus = payment?.PaymentStatus ?? "Pending",
                PaidAmount = payment?.PaidAmount ?? 0,

                HasQrPass = qrPass != null,
                CompletionPercentage = progress
            });
        }

        // =========================
        // REGISTER EVENT (ONLY REGISTRATION - NO PAYMENT HERE)
        // =========================
        public async Task<IActionResult> RegisterEvent(int eventId)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == int.Parse(userId));

            if (participant == null)
                return RedirectToAction("Dashboard");

            var existing = await _context.Registration
                .FirstOrDefaultAsync(r =>
                    r.ParticipantId == participant.ParticipantId &&
                    r.EventId == eventId);

            if (existing != null)
                return RedirectToAction("MyRegistration");

            var registration = new Registration
            {
                EventId = eventId,
                ParticipantId = participant.ParticipantId,
                RegistrationDate = DateTime.Now,
                RegistrationStatus = "Pending"
            };

            _context.Registration.Add(registration);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyRegistration");
        }
        // =========================
        // MY REGISTRATION
        // =========================
        public async Task<IActionResult> MyRegistration()
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
                return RedirectToAction("Login", "Account");

            var registration = await _context.Registration
                .Include(r => r.Event)
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            return View(registration);
        }

        // =========================
        // PAYMENT PAGE (ONLY READ)
        // =========================
        public async Task<IActionResult> MyPayment()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == int.Parse(userId));

            if (participant == null)
                return RedirectToAction("Dashboard");

            var registration = await _context.Registration
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationId)
                .FirstOrDefaultAsync();

            if (registration == null)
                return View(null);

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.RegistrationId == registration.RegistrationId);

            return View(payment);
        }

        // =========================
        // MY GUEST
        // =========================
        public async Task<IActionResult> MyGuest()
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
                return RedirectToAction("Login", "Account");

            var registration = await _context.Registration
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            if (registration == null)
                return View(new List<Guest>());

            var guests = await _context.Guest
                .Where(g => g.RegistrationId == registration.RegistrationId)
                .ToListAsync();

            return View(guests);
        }

        // =========================
        // LOGOUT SAFE
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}