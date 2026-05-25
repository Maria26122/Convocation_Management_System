using Convocation.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class StaffController : Controller
    {
        private readonly ConvocationDbContext _context;

        public StaffController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(userIdString);

            var assignedTasks = await _context.StaffTask
                .Include(s => s.DistributionTask)
                .ThenInclude(t => t.Event)
                .Where(s => s.UserAccountId == userId)
                .ToListAsync();

            ViewBag.TotalTasks = assignedTasks.Count;

            return View(assignedTasks);
        }
    }
}