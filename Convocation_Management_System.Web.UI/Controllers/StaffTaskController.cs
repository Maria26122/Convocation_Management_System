using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class StaffTaskController : Controller
    {
        private readonly ConvocationDbContext _context;

        public StaffTaskController(ConvocationDbContext context)
        {
            _context = context;
        }

        private string CurrentRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();
        }

        private int? CurrentUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (int.TryParse(userId, out int id))
                return id;

            return null;
        }

        private bool CanManage()
        {
            var role = CurrentRole();
            return role == "admin" || role == "eventmanager";
        }

        private bool IsStaff()
        {
            return CurrentRole() == "staff";
        }

        private async Task LoadDropdownsAsync(object? selectedEventId = null, object? selectedStaffId = null)
        {
            var events = await _context.Event
                .Where(e => e.IsActive)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var staffUsers = await _context.UserAccount
                .Include(u => u.Role)
                .Where(u => u.Role != null &&
                            u.Role.RoleName.ToLower() == "staff" &&
                            u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.EventId = new SelectList(events, "EventId", "EventTitle", selectedEventId);
            ViewBag.StaffUserAccountId = new SelectList(staffUsers, "UserAccountId", "FullName", selectedStaffId);
        }

        public async Task<IActionResult> Index()
        {
            if (!CanManage() && !IsStaff())
                return RedirectToAction("Login", "Account");

            var userId = CurrentUserId();

            var query = _context.StaffTask
                .Include(s => s.Event)
                .Include(s => s.StaffUserAccount)
                .AsQueryable();

            if (IsStaff())
            {
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                query = query.Where(s => s.StaffUserAccountId == userId.Value);
            }

            var tasks = await query
                .OrderByDescending(s => s.AssignedAt)
                .ToListAsync();

            return View(tasks);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!CanManage() && !IsStaff())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var task = await _context.StaffTask
                .Include(s => s.Event)
                .Include(s => s.StaffUserAccount)
                .FirstOrDefaultAsync(s => s.StaffTaskId == id.Value);

            if (task == null)
                return NotFound();

            if (IsStaff())
            {
                var userId = CurrentUserId();

                if (userId == null || task.StaffUserAccountId != userId.Value)
                    return RedirectToAction("Login", "Account");
            }

            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!CanManage())
                return RedirectToAction("Login", "Account");

            await LoadDropdownsAsync();

            return View(new StaffTask
            {
                Status = "Assigned",
                AssignedAt = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffTask staffTask)
        {
            if (!CanManage())
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Event");
            ModelState.Remove("StaffUserAccount");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(staffTask.EventId, staffTask.StaffUserAccountId);
                return View(staffTask);
            }

            staffTask.Status = "Assigned";
            staffTask.AssignedAt = DateTime.Now;
            staffTask.CompletedAt = null;

            _context.StaffTask.Add(staffTask);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Staff task assigned successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!CanManage())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var task = await _context.StaffTask.FindAsync(id.Value);

            if (task == null)
                return NotFound();

            await LoadDropdownsAsync(task.EventId, task.StaffUserAccountId);
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StaffTask staffTask)
        {
            if (!CanManage())
                return RedirectToAction("Login", "Account");

            if (id != staffTask.StaffTaskId)
                return NotFound();

            ModelState.Remove("Event");
            ModelState.Remove("StaffUserAccount");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(staffTask.EventId, staffTask.StaffUserAccountId);
                return View(staffTask);
            }

            var existing = await _context.StaffTask.FindAsync(id);

            if (existing == null)
                return NotFound();

            existing.EventId = staffTask.EventId;
            existing.StaffUserAccountId = staffTask.StaffUserAccountId;
            existing.TaskName = staffTask.TaskName;
            existing.Description = staffTask.Description;
            existing.Status = staffTask.Status;

            if (staffTask.Status == "Completed" && existing.CompletedAt == null)
                existing.CompletedAt = DateTime.Now;

            if (staffTask.Status != "Completed")
                existing.CompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Staff task updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            if (!CanManage() && !IsStaff())
                return RedirectToAction("Login", "Account");

            var task = await _context.StaffTask.FindAsync(id);

            if (task == null)
                return NotFound();

            if (IsStaff())
            {
                var userId = CurrentUserId();

                if (userId == null || task.StaffUserAccountId != userId.Value)
                    return RedirectToAction("Login", "Account");
            }

            task.Status = "Completed";
            task.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Task marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!CanManage())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var task = await _context.StaffTask
                .Include(s => s.Event)
                .Include(s => s.StaffUserAccount)
                .FirstOrDefaultAsync(s => s.StaffTaskId == id.Value);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!CanManage())
                return RedirectToAction("Login", "Account");

            var task = await _context.StaffTask.FindAsync(id);

            if (task == null)
                return NotFound();

            _context.StaffTask.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Staff task deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}