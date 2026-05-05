using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class DistributionLogController : Controller
    {
        private readonly ConvocationDbContext _context;

        public DistributionLogController(ConvocationDbContext context)
        {
            _context = context;
        }

        private string CurrentRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();
        }

        private bool HasAccess()
        {
            var role = CurrentRole();
            return role == "admin" || role == "staff" || role == "eventmanager";
        }

        private int? CurrentUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return int.TryParse(userId, out int id) ? id : null;
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
                                  + " - " + (r.Participant != null && r.Participant.UserAccount != null
                                      ? r.Participant.UserAccount.FullName
                                      : "Unknown")
                                  + " - " + (r.Event != null ? r.Event.EventTitle : "No Event")
                })
                .ToListAsync();

            ViewBag.RegistrationId = new SelectList(registrations, "RegistrationId", "DisplayText", selectedId);
        }

        public async Task<IActionResult> Index()
        {
            if (!HasAccess())
                return RedirectToAction("Login", "Account");

            var logs = await _context.DistributionLog
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Event)
                .Include(d => d.UserAccount)
                .OrderByDescending(d => d.ActionDate)
                .ThenByDescending(d => d.DistributionLogId)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!HasAccess())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var log = await _context.DistributionLog
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Event)
                .Include(d => d.UserAccount)
                .FirstOrDefaultAsync(d => d.DistributionLogId == id.Value);

            if (log == null)
                return NotFound();

            return View(log);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!HasAccess())
                return RedirectToAction("Login", "Account");

            await LoadRegistrationDropdownAsync();

            return View(new DistributionLog
            {
                ActionDate = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionLog model)
        {
            if (!HasAccess())
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Registration");
            ModelState.Remove("UserAccount");

            var registration = await _context.Registration
                .FirstOrDefaultAsync(r => r.RegistrationId == model.RegistrationId);

            if (registration == null)
                ModelState.AddModelError("RegistrationId", "Invalid registration.");

            if (registration != null && registration.RegistrationStatus != "Paid")
                ModelState.AddModelError("RegistrationId", "Distribution is allowed only after payment.");

            if (string.IsNullOrWhiteSpace(model.ActionType))
                ModelState.AddModelError("ActionType", "Select distribution item.");

            bool alreadyExists = await _context.DistributionLog
                .AnyAsync(d => d.RegistrationId == model.RegistrationId &&
                               d.ActionType == model.ActionType);

            if (alreadyExists)
                ModelState.AddModelError("ActionType", model.ActionType + " is already distributed for this registration.");

            if (!ModelState.IsValid)
            {
                await LoadRegistrationDropdownAsync(model.RegistrationId);
                return View(model);
            }

            model.UserAccountId = CurrentUserId();
            model.ActionDate = DateTime.Now;
            model.Note = model.ActionType + " Distribution";

            _context.DistributionLog.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = model.ActionType + " distributed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasAccess())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var log = await _context.DistributionLog
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Event)
                .Include(d => d.UserAccount)
                .FirstOrDefaultAsync(d => d.DistributionLogId == id.Value);

            if (log == null)
                return NotFound();

            return View(log);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!HasAccess())
                return RedirectToAction("Login", "Account");

            var log = await _context.DistributionLog.FindAsync(id);

            if (log == null)
                return NotFound();

            _context.DistributionLog.Remove(log);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Distribution log deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}