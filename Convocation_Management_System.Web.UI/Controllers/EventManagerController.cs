using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            var user = await _context.UserAccount.FindAsync(userId);

            var model = new EventManagerDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email,

                TotalEvents = await _context.Event.CountAsync(),
                TotalTasks = await _context.DistributionTask.CountAsync(),
                PendingTasks = await _context.DistributionTask.CountAsync(x => x.Status == "Pending"),
                CompletedTasks = await _context.DistributionTask.CountAsync(x => x.Status == "Completed"),

                TotalStaff = await _context.UserAccount
                    .CountAsync(x => x.Role.RoleName == "Staff")
            };

            return View(model);
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