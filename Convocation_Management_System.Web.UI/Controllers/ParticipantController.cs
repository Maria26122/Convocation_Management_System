using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        // SESSION HELPERS
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

        // =========================
        // INDEX (ADMIN)
        // =========================
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participant
                .Include(p => p.UserAccount)
                .OrderByDescending(p => p.ParticipantId)
                .ToListAsync();

            return View(participants);
        }

        // =========================
        // CREATE (ADMIN)
        // =========================
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var usedUserIds = await _context.Participant
                .Select(p => p.UserAccountId)
                .ToListAsync();

            ViewBag.UserAccountId = new SelectList(
                await _context.UserAccount
                    .Include(u => u.Role)
                    .Where(u =>
                        (u.Role.RoleName == "Student" || u.Role.RoleName == "Participant") &&
                        !usedUserIds.Contains(u.UserAccountId))
                    .ToListAsync(),
                "UserAccountId",
                "FullName");

            return View();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Participant participant)
        {
            if (!ModelState.IsValid)
            {
                return await ReloadCreate(participant);
            }

            var exists = await _context.Participant
                .AnyAsync(p => p.UserAccountId == participant.UserAccountId);

            if (exists)
            {
                ModelState.AddModelError("", "This user already has a participant profile.");
                return await ReloadCreate(participant);
            }

            participant.CreatedAt = DateTime.UtcNow;

            _context.Participant.Add(participant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Participant created successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> ReloadCreate(Participant participant)
        {
            var usedUserIds = await _context.Participant
                .Select(p => p.UserAccountId)
                .ToListAsync();

            ViewBag.UserAccountId = new SelectList(
                await _context.UserAccount
                    .Include(u => u.Role)
                    .Where(u =>
                        (u.Role.RoleName == "Student" || u.Role.RoleName == "Participant") &&
                        !usedUserIds.Contains(u.UserAccountId))
                    .ToListAsync(),
                "UserAccountId",
                "FullName",
                participant.UserAccountId);

            return View(participant);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest();

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null)
                return NotFound();

            return View(participant);
        }

        // =========================
        // EDIT (ADMIN)
        // =========================
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var participant = await _context.Participant.FindAsync(id);
            if (participant == null) return NotFound();

            return View(participant);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Participant participant)
        {
            if (id != participant.ParticipantId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(participant);

            var existing = await _context.Participant.FindAsync(id);
            if (existing == null) return NotFound();

            existing.StudentId = participant.StudentId;
            existing.Department = participant.Department;
            existing.Program = participant.Program;
            existing.Session = participant.Session;
            existing.IsEligible = participant.IsEligible;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Participant updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE (ADMIN)
        // =========================
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null)
                return NotFound();

            return View(participant);
        }

        [Authorize(Roles = "admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var participant = await _context.Participant.FindAsync(id);

            if (participant != null)
            {
                _context.Participant.Remove(participant);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Participant deleted successfully.";
            return RedirectToAction(nameof(Index));
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
                return RedirectToAction("Login", "Account");

            var registration = await _context.Registration
                .Include(r => r.Event)
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            var payment = registration == null
                ? null
                : await _context.Payment.FirstOrDefaultAsync(p =>
                    p.RegistrationId == registration.RegistrationId);

            return View(new ParticipantDashboardViewModel
            {
                FullName = participant.UserAccount.FullName,
                Email = participant.UserAccount.Email,
                RegistrationStatus = registration?.RegistrationStatus ?? "Not Registered",
                EventTitle = registration?.Event?.EventTitle ?? "No Event",
                PaymentStatus = payment?.PaymentStatus ?? "Pending",
                PaidAmount = payment?.PaidAmount ?? 0
            });
        }

        // =========================
        // MY REGISTRATION
        // =========================
        public async Task<IActionResult> MyRegistration()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null) return NotFound();

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
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null) return NotFound();

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
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null)
                return View(new List<Guest>());

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
        // MY QR PASS
        // =========================
        public async Task<IActionResult> MyQrPass(int registrationId)
        {
            var qr = await _context.QrPass
                .Include(q => q.Registration)
                .FirstOrDefaultAsync(q => q.RegistrationId == registrationId);

            if (qr == null)
                return NotFound();

            return View(qr);
        }

        // =========================
        // MY PROFILE
        // =========================
        public async Task<IActionResult> MyProfile()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var participant = await _context.Participant
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            if (participant == null)
                return RedirectToAction("Dashboard");

            return View(participant);
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