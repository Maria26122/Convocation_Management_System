using Convocation.DataAccess;
using Convocation.Entities;
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
            if (string.IsNullOrEmpty(userId))
                return null;

            if (!int.TryParse(userId, out int parsed))
                return null;

            return parsed;
        }

        // =========================
        // ADMIN PARTICIPANT LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            var participants = await _context.Participants
                .Include(p => p.UserAccount)
                .OrderByDescending(p => p.ParticipantId)
                .ToListAsync();

            return View(participants);
        }

        // =========================
        // STUDENT DASHBOARD
        // =========================
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
                ? new List<Guest>()
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

            var latestRegistration = await _context.Registrations
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            if (latestRegistration == null)
                return View(null);

            var payment = await _context.Payments
                .Include(p => p.Registration)
                .FirstOrDefaultAsync(p => p.RegistrationId == latestRegistration.RegistrationId);

            if (payment == null)
            {
                payment = new Payment
                {
                    RegistrationId = latestRegistration.RegistrationId,
                    Registration = latestRegistration,
                    PaidAmount = latestRegistration.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentMethod = "SSLCommerz",
                    TransactionId = null,
                    PaymentDate = null,
                    SessionKey = null
                };
            }

            return View(payment);
        }

        public async Task<IActionResult> MyQrPass()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
                return View(null);

            var qr = await _context.QrPasses
                .Include(q => q.Registration)
                .FirstOrDefaultAsync(q => q.Registration != null &&
                                          q.Registration.ParticipantId == participant.ParticipantId);

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
                return View(new List<Guest>());

            var guests = await _context.Guests
                .Where(g => g.RegistrationId == registration.RegistrationId)
                .ToListAsync();

            return View(guests);
        }
    }
}