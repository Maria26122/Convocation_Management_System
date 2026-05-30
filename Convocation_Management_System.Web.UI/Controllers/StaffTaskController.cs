using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "admin,eventmanager")]
    public class StaffTaskController : Controller
    {
        private readonly ConvocationDbContext _context;

        public StaffTaskController(ConvocationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.StaffTask
                .Include(t => t.UserAccount)
                .Include(t => t.DistributionTask)
                .OrderByDescending(t => t.AssignedAt)
                .ToListAsync();

            return View(tasks);
        }

        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            var model = new StaffTaskViewModel();

            LoadDropdowns(model);

            return View(model);
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            // validate distribution task exists
            var taskExists = await _context.DistributionTask
                .AnyAsync(t => t.DistributionTaskId == model.DistributionTaskId);

            if (!taskExists)
            {
                ModelState.AddModelError("", "Invalid Distribution Task selected.");
                LoadDropdowns(model);
                return View(model);
            }

            // validate staff exists
            var staffExists = await _context.UserAccount
                .AnyAsync(u => u.UserAccountId == model.UserAccountId);

            if (!staffExists)
            {
                ModelState.AddModelError("", "Invalid Staff selected.");
                LoadDropdowns(model);
                return View(model);
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

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.StaffTask
                .FirstOrDefaultAsync(x => x.StaffTaskId == id);

            if (task == null)
                return NotFound();

            var model = new StaffTaskViewModel
            {
                StaffTaskId = task.StaffTaskId,
                DistributionTaskId = task.DistributionTaskId,
                UserAccountId = task.UserAccountId,
                Status = task.Status,
                AssignedAt = task.AssignedAt,
                CompletedAt = task.CompletedAt
            };

            LoadDropdowns(model);

            return View(model);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StaffTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            var task = await _context.StaffTask
                .FirstOrDefaultAsync(x => x.StaffTaskId == model.StaffTaskId);

            if (task == null)
                return NotFound();

            task.DistributionTaskId = model.DistributionTaskId;
            task.UserAccountId = model.UserAccountId;
            task.Status = model.Status;

            if (model.Status == "Completed" && task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Task updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.StaffTask
                .Include(t => t.UserAccount)
                .Include(t => t.DistributionTask)
                .FirstOrDefaultAsync(t => t.StaffTaskId == id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.StaffTask.FindAsync(id);

            if (task != null)
            {
                _context.StaffTask.Remove(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DROPDOWNS (FIXED)
        // =========================
        private void LoadDropdowns(StaffTaskViewModel model)
        {
            model.DistributionTasks = _context.DistributionTask
                .Select(t => new SelectListItem
                {
                    Value = t.DistributionTaskId.ToString(),
                    Text = t.TaskTitle
                })
                .ToList();

            model.staffs = _context.UserAccount
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Staff")
                .Select(u => new SelectListItem
                {
                    Value = u.UserAccountId.ToString(),
                    Text = u.FullName
                })
                .ToList();
        }
    }
}