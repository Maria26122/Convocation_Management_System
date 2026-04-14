using Convocation.DataAccess;
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
            return !string.IsNullOrEmpty(role) && role.Trim().ToLower() == "student";
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

            return View(participant);
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

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.RegistrationId == participant.ParticipantId);

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
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction("Login", "Account");
            }

            var qrPass = await _context.QrPasses
                .FirstOrDefaultAsync(q => q.RegistrationId == participant.ParticipantId);

            return View(qrPass);
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

            var guests = await _context.Guests
                .Where(g => g.RegistrationId == participant.ParticipantId)
                .ToListAsync();

            return View(guests);
        }
    }
}