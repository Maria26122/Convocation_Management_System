using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "eventmanager")]
    public class EventManagerController : Controller
    {
        private readonly ConvocationDbContext _context;

        public EventManagerController(ConvocationDbContext context)
        {
            _context = context;
        }

        // ==========================
        // DASHBOARD
        // ==========================
        public async Task<IActionResult> Dashboard()
        {
            var totalTasks = await _context.DistributionTask.CountAsync();

            var pendingTasks = await _context.DistributionTask
                .CountAsync(x => x.Status == "Pending");

            var completedTasks = await _context.DistributionTask
                .CountAsync(x => x.Status == "Completed");

            var totalStaff = await _context.UserAccount
                .CountAsync(x => x.Role.RoleName == "staff");

            ViewBag.TotalTasks = totalTasks;
            ViewBag.PendingTasks = pendingTasks;
            ViewBag.CompletedTasks = completedTasks;
            ViewBag.TotalStaff = totalStaff;

            return View();
        }

            
        // ==========================
        // VIEW TASKS
        // ==========================
        public async Task<IActionResult> Tasks()
        {
            var tasks = await _context.DistributionTask
                .OrderByDescending(x => x.DistributionTaskId)
                .ToListAsync();

            return View(tasks);
        }

        // ==========================
        // VIEW STAFF
        // ==========================
        public async Task<IActionResult> Staffs()
        {
            var staffs = await _context.UserAccount
                .Include(x => x.Role)
                .Where(x =>
                    x.Role.RoleName.ToLower() == "staff")
                .ToListAsync();

            return View(staffs);
        }

        public IActionResult AssignStaff()
        {
            var model = new StaffTaskViewModel
            {
                DistributionTasks = _context.DistributionTask
                    .Select(t => new SelectListItem
                    {
                        Value = t.DistributionTaskId.ToString(),
                        Text = t.TaskTitle
                    }).ToList(),

                staffs = _context.UserAccount
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName.ToLower() == "staff")
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserAccountId.ToString(),
                        Text = u.FullName
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStaff(StaffTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AssignStaff));
            }

            var exists = await _context.StaffTask.AnyAsync(x =>
                x.UserAccountId == model.UserAccountId &&
                x.DistributionTaskId == model.DistributionTaskId);

            if (exists)
            {
                TempData["Error"] = "Already assigned!";
                return RedirectToAction(nameof(AssignStaff));
            }

            var staffTask = new StaffTask
            {
                DistributionTaskId = model.DistributionTaskId,
                UserAccountId = model.UserAccountId,
                Status = "Pending",
                AssignedAt = DateTime.Now
            };

            _context.StaffTask.Add(staffTask);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Staff assigned successfully.";
            return RedirectToAction(nameof(Tasks));
        }

        // ==========================
        // VIEW LOGS
        // ==========================
        public async Task<IActionResult> Logs()
        {
            var logs = await _context.DistributionLog
                .Include(x => x.Participant)
                .Include(x => x.UserAccount)
                .Include(x => x.DistributionTask)
                .OrderByDescending(x => x.ActionDate)
                .ToListAsync();

            return View(logs);
        }
    }
}