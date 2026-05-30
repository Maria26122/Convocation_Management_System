using Convocation.DataAccess;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "staff")]
    public class StaffController : Controller
    {

        private readonly ConvocationDbContext _context;

        public StaffController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            var user = await _context.UserAccount.FindAsync(userId);

            var model = new StaffDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email,

                AssignedTasks = await _context.StaffTask
                    .CountAsync(x => x.UserAccountId == userId),

                PendingTasks = await _context.StaffTask
                    .CountAsync(x => x.UserAccountId == userId && x.Status == "Pending"),

                InProgressTasks = await _context.StaffTask
                    .CountAsync(x => x.UserAccountId == userId && x.Status == "InProgress"),

                CompletedTasks = await _context.StaffTask
                    .CountAsync(x => x.UserAccountId == userId && x.Status == "Completed"),

                TodayScans = await _context.DistributionLog
                    .CountAsync(x => x.UserAccountId == userId && x.ActionDate.Date == DateTime.Today)
            };

            return View(model);
        }
    }
}