using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
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

        private bool IsAdmin()
        {
            return CurrentRole() == "admin";
        }

        private bool IsStaff()
        {
            return CurrentRole() == "staff" || CurrentRole() == "eventmanager";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            var logs = await _context.DistributionLogs
                .Include(d => d.Participant)
                    .ThenInclude(p => p!.UserAccount)
                .Include(d => d.Registration)
                .OrderByDescending(d => d.DistributedAt)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> Create()
        {
            if (!IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            var vm = new DistributionLogCreateViewModel
            {
                Participant = await _context.Participants
                    .Include(p => p.UserAccount)
                    .Select(p => new SelectListItem
                    {
                        Value = p.ParticipantId.ToString(),
                        Text = p.StudentId + " - " + (p.UserAccount != null ? p.UserAccount.FullName : p.Department)
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionLogCreateViewModel vm)
        {
            if (!IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                vm.Participant = await _context.Participants
                    .Include(p => p.UserAccount)
                    .Select(p => new SelectListItem
                    {
                        Value = p.ParticipantId.ToString(),
                        Text = p.StudentId + " - " + (p.UserAccount != null ? p.UserAccount.FullName : p.Department)
                    })
                    .ToListAsync();

                return View(vm);
            }

            var latestRegistration = await _context.Registrations
                .Where(r => r.ParticipantId == vm.ParticipantId)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            var log = new DistributionLog
            {
                ParticipantId = vm.ParticipantId,
                RegistrationId = latestRegistration?.RegistrationId,
                ItemName = vm.ItemName,
                DistributedAt = DateTime.Now,
                DistributedBy = vm.DistributedBy,
                Remarks = vm.Remarks
            };

            _context.DistributionLogs.Add(log);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Distribution log saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}