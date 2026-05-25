using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
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

      
        // =========================
        // HELPERS (SESSION ONLY)
        // =========================

        private string GetRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();
        }

        private bool IsParticipant()
        {
            var role = GetRole();
            return role == "participant" || role == "student";
        }

        private int? GetUserId()
        {
            var id = HttpContext.Session.GetString("UserId");
            return int.TryParse(id, out int val) ? val : null;
        }

        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participant
                .Include(p => p.UserAccount)
                .OrderByDescending(p => p.ParticipantId)
                .ToListAsync();

            return View(participants);
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Dashboard()
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
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
                : await _context.Payment.FirstOrDefaultAsync(p =>
                    p.RegistrationId == registration.RegistrationId);

            var model = new ParticipantDashboardViewModel
            {
                FullName = participant.UserAccount.FullName,
                Email = participant.UserAccount.Email,

                RegistrationStatus = registration?.RegistrationStatus ?? "Not Registered",
                EventTitle = registration?.Event?.EventTitle ?? "No Event",

                PaymentStatus = payment?.PaymentStatus ?? "Pending",
                PaidAmount = payment?.PaidAmount ?? 0
            };

            return View(model);
        }

        // =========================
        // REGISTER EVENT
        // =========================
        public async Task<IActionResult> RegisterEvent(int eventId)
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

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
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

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
        // MY PAYMENT
        // =========================
        public async Task<IActionResult> MyPayment()
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null)
                return RedirectToAction("Login", "Account");

            var registration = await _context.Registration
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
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
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

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
        // MY PROFILE
        // =========================
        public async Task<IActionResult> MyProfile()
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null)
            {
                TempData["Error"] = "Profile not found";
                return RedirectToAction("Dashboard");
            }

            return View(participant);
        }

        // =========================
        // MY QR PASS
        // =========================
        public async Task<IActionResult> MyQrPass(int registrationId)
        {
            var qr = await _context.QrPass
                .Include(q => q.Registration)
                .FirstOrDefaultAsync(q => q.RegistrationId == registrationId);

            return View(qr);
        }

        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}