using Convocation.DataAccess;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "admin,event manager")]
    public class OperationController : Controller
    {
        private readonly ConvocationDbContext _context;

        public OperationController(ConvocationDbContext context)
        {
            _context = context;
        }

     
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            var model = new OperationDashboardViewModel
            {
                TotalTasks = await _context.DistributionTask.CountAsync(),

                ActiveTasks = await _context.DistributionTask
                    .CountAsync(x => x.Status == "In Progress"),

                CompletedTasks = await _context.DistributionTask
                    .CountAsync(x => x.Status == "Completed"),

                TotalDistributedToday = await _context.DistributionLog
                    .CountAsync(x => x.ActionDate.Date == today),

                PendingDistribution = await _context.DistributionTask
                    .CountAsync(x => x.Status != "Completed"),

                FoodCount = await _context.DistributionLog
                    .CountAsync(x => x.ActionType == "Food"),

                GownCount = await _context.DistributionLog
                    .CountAsync(x => x.ActionType == "Gown"),

                CertificateCount = await _context.DistributionLog
                    .CountAsync(x => x.ActionType == "Certificate"),

                KitCount = await _context.DistributionLog
                    .CountAsync(x => x.ActionType == "Kit"),

                ActiveStaff = await _context.UserAccount
                    .CountAsync(x => x.Role.RoleName == "Staff")
            };

            return View(model);
        }

        public async Task<IActionResult> LiveFeed()
        {
            var logs = await _context.OperationActivityLog
                .OrderByDescending(x => x.Time)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}