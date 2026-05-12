using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

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

        private async Task LoadUserDropdownAsync(object? selectedUserId = null)
        {
            var studentUsers = await _context.UserAccount
                .Include(u => u.Role)
                .Where(u => u.Role != null &&
                            (u.Role.RoleName.ToLower() == "student" ||
                             u.Role.RoleName.ToLower() == "participant"))
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.UserAccountId,
                    DisplayText = u.FullName + " (" + u.Email + ")"
                })
                .ToListAsync();

            ViewBag.UserAccountId = new SelectList(studentUsers, "UserAccountId", "DisplayText", selectedUserId);
        }

        // =========================
        // ADMIN PARTICIPANT LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            var participants = await _context.Participant
                .Include(p => p.UserAccount)
                .OrderByDescending(p => p.ParticipantId)
                .ToListAsync();

            return View(participants);
        }

        // =========================
        // ADMIN DETAILS
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.ParticipantId == id.Value);

            if (participant == null)
                return NotFound();

            return View(participant);
        }

        // =========================
        // ADMIN CREATE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            await LoadUserDropdownAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Participant participant)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (participant.UserAccountId <= 0)
            {
                ModelState.AddModelError("UserAccountId", "Please select a student account.");
            }

            bool userAlreadyAssigned = await _context.Participant
                .AnyAsync(p => p.UserAccountId == participant.UserAccountId);

            if (userAlreadyAssigned)
            {
                ModelState.AddModelError("UserAccountId", "This user already has a participant profile.");
            }

            if (!ModelState.IsValid)
            {
                await LoadUserDropdownAsync(participant.UserAccountId);
                return View(participant);
            }

            participant.CreatedAt = DateTime.Now;

            _context.Participant.Add(participant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Participant created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ADMIN EDIT
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var participant = await _context.Participant.FindAsync(id.Value);
            if (participant == null)
                return NotFound();

            await LoadUserDropdownAsync(participant.UserAccountId);
            return View(participant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Participant participant)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id != participant.ParticipantId)
                return NotFound();

            bool userAlreadyAssignedToAnother = await _context.Participant
                .AnyAsync(p => p.UserAccountId == participant.UserAccountId &&
                               p.ParticipantId != participant.ParticipantId);

            if (userAlreadyAssignedToAnother)
            {
                ModelState.AddModelError("UserAccountId", "This user is already linked to another participant profile.");
            }

            if (!ModelState.IsValid)
            {
                await LoadUserDropdownAsync(participant.UserAccountId);
                return View(participant);
            }

            try
            {
                var existingParticipant = await _context.Participant
                    .FirstOrDefaultAsync(p => p.ParticipantId == id);

                if (existingParticipant == null)
                    return NotFound();

                existingParticipant.UserAccountId = participant.UserAccountId;
                existingParticipant.StudentId = participant.StudentId;
                existingParticipant.Department = participant.Department;
                existingParticipant.Program = participant.Program;
                existingParticipant.Session = participant.Session;
                existingParticipant.IsEligible = participant.IsEligible;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Participant updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to update participant.");
                await LoadUserDropdownAsync(participant.UserAccountId);
                return View(participant);
            }
        }

        // =========================
        // ADMIN DELETE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.ParticipantId == id.Value);

            if (participant == null)
                return NotFound();

            return View(participant);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();

            if (role != "admin")
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null)
                return NotFound();

            var registrations = await _context.Registration
                .Where(r => r.ParticipantId == id)
                .ToListAsync();

            foreach (var registration in registrations)
            {
                var payments = await _context.Payment
                    .Where(p => p.RegistrationId == registration.RegistrationId)
                    .ToListAsync();

                var qrPasses = await _context.QrPass
                    .Where(q => q.RegistrationId == registration.RegistrationId)
                    .ToListAsync();

                var guests = await _context.Guest
                    .Where(g => g.RegistrationId == registration.RegistrationId)
                    .ToListAsync();

                var distributionLogs = await _context.DistributionLog
                    .Where(d => d.RegistrationId == registration.RegistrationId)
                    .ToListAsync();

                _context.Payment.RemoveRange(payments);
                _context.QrPass.RemoveRange(qrPasses);
                _context.Guest.RemoveRange(guests);
                _context.DistributionLog.RemoveRange(distributionLogs);
            }

            _context.Registration.RemoveRange(registrations);

            _context.Participant.Remove(participant);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Participant and related records deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // STUDENT DASHBOARD
        // =========================

        [HttpPost]
        public async Task<IActionResult> RegisterEvent(int eventId)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == int.Parse(userId));

            if (participant == null)
                return RedirectToAction("Dashboard");

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
        public async Task<IActionResult> Dashboard()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

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

            var participant = await _context.Participant
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

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

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

            return View(registration);
        }

        public async Task<IActionResult> MyPayment()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedInUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var latestRegistration = await _context.Registration
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            if (latestRegistration == null)
                return View(null);

            var payment = await _context.Payment
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

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
                return View(null);

            var qr = await _context.QrPass
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

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId.Value);

            if (participant == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

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
    }
}