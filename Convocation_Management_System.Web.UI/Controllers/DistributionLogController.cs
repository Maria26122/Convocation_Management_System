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

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin" && role != "Staff")
            {
                return RedirectToAction("Login", "Account");
            }
            var logs = await _context.DistributionLogs
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Event)
                .Include(d => d.UserAccount)
                .OrderByDescending(d => d.ActionDate)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.DistributionLogs
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Event)
                .Include(d => d.UserAccount)
                .FirstOrDefaultAsync(d => d.DistributionLogId == id);

            if (log == null) return NotFound();

            return View(log);
        }

        public IActionResult Create()
        {
            LoadDropdowns();
            return View(new DistributionLog
            {
                ActionDate = DateTime.Now,
                ActionType = "Entry"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionLog distributionLog)
        {
            if (!await _context.Registrations.AnyAsync(r => r.RegistrationId == distributionLog.RegistrationId))
            {
                ModelState.AddModelError("RegistrationId", "Please select a valid registration.");
            }

            if (!await _context.UserAccounts.AnyAsync(u => u.UserAccountId == distributionLog.UserAccountId))
            {
                ModelState.AddModelError("StaffUserAccountId", "Please select a valid staff/user.");
            }

            if (ModelState.IsValid)
            {
                _context.DistributionLogs.Add(distributionLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(distributionLog.RegistrationId, distributionLog.UserAccountId);
            return View(distributionLog);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.DistributionLogs.FindAsync(id);
            if (log == null) return NotFound();

            LoadDropdowns(log.RegistrationId, log.UserAccountId);
            return View(log);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DistributionLog distributionLog)
        {
            if (id != distributionLog.DistributionLogId) return NotFound();

            if (!await _context.Registrations.AnyAsync(r => r.RegistrationId == distributionLog.RegistrationId))
            {
                ModelState.AddModelError("RegistrationId", "Please select a valid registration.");
            }

            if (!await _context.UserAccounts.AnyAsync(u => u.UserAccountId == distributionLog.UserAccountId))
            {
                ModelState.AddModelError("StaffUserAccountId", "Please select a valid staff/user.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(distributionLog);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.DistributionLogs.AnyAsync(d => d.DistributionLogId == distributionLog.DistributionLogId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            LoadDropdowns(distributionLog.RegistrationId, distributionLog.UserAccountId);
            return View(distributionLog);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.DistributionLogs
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(d => d.Registration)
                    .ThenInclude(r => r.Event)
                .Include(d => d.UserAccount)
                .FirstOrDefaultAsync(d => d.DistributionLogId == id);

            if (log == null) return NotFound();

            return View(log);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var log = await _context.DistributionLogs.FindAsync(id);
            if (log != null)
            {
                _context.DistributionLogs.Remove(log);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private void LoadDropdowns(object? selectedRegistration = null, object? selectedStaff = null)
        {
            var registrations = _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .AsEnumerable()
                .Select(r => new
                {
                    r.RegistrationId,
                    DisplayText = $"Reg #{r.RegistrationId} - {r.Participant?.StudentId ?? "No Student"} - {r.Event?.EventTitle ?? "No Event"}"
                })
                .ToList();

            var staffUsers = _context.UserAccounts
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.UserAccountId,
                    DisplayText = u.FullName + " - " + u.Email
                })
                .ToList();

            ViewBag.RegistrationId = new SelectList(registrations, "RegistrationId", "DisplayText", selectedRegistration);
            ViewBag.StaffUserAccountId = new SelectList(staffUsers, "UserAccountId", "DisplayText", selectedStaff);
        }
    }
}