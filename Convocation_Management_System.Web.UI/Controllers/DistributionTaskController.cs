using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "admin,eventmanager,staff")]
    public class DistributionTaskController : Controller
    {
        private readonly ConvocationDbContext _context;

        public DistributionTaskController(ConvocationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.DistributionTask
                .Include(t => t.Event)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        // =========================
        // CREATE
        // =========================
        public IActionResult Create()
        {
            var model = new DistributionTaskViewModel();
            LoadDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            var task = new DistributionTask
            {
                EventId = model.EventId,
                TaskTitle = model.TaskTitle,
                Description = model.Description,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.DistributionTask.Add(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Distribution task created.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.DistributionTask
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DistributionTaskId == id);

            if (task == null)
                return NotFound();

            var model = new DistributionTaskViewModel
            {
                DistributionTaskId = task.DistributionTaskId,
                EventId = task.EventId,
                TaskTitle = task.TaskTitle,
                Description = task.Description,
                Status = task.Status,
            };

            LoadDropdowns(model);
            return View(model);
        }

        // =========================
        // EDIT (POST FIXED)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DistributionTaskViewModel model)
        {
            if (model.DistributionTaskId <= 0)
            {
                TempData["Error"] = "Invalid task ID.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            var task = await _context.DistributionTask
          .FirstOrDefaultAsync(x => x.DistributionTaskId == model.DistributionTaskId);

            if (task == null)
                return NotFound();

            // VALIDATE EVENT
            var eventExists = await _context.Event.AnyAsync(e => e.EventId == model.EventId);
            if (!eventExists)
            {
                TempData["Error"] = "Invalid Event selected.";
                return RedirectToAction(nameof(Index));
            }

            // UPDATE
            task.EventId = model.EventId;
            task.TaskTitle = model.TaskTitle;
            task.Description = model.Description;
            task.Status = model.Status;

            if (model.Status == "Completed")
                task.CompletedAt ??= DateTime.Now;
            else
                task.CompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Task updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.DistributionTask
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.DistributionTaskId == id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.DistributionTask.FindAsync(id);

            if (task != null)
            {
                _context.DistributionTask.Remove(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // START TASK
        // =========================
        public async Task<IActionResult> StartTask(int id)
        {
            var task = await _context.DistributionTask.FindAsync(id);

            if (task == null)
                return NotFound();

            task.Status = "In Progress";
            _context.Update(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DROPDOWNS
        // =========================
        private void LoadDropdowns(DistributionTaskViewModel model)
        {
            model.Events = _context.Event
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventTitle
                }).ToList();

            model.Staffs = _context.UserAccount
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Staff")
                .Select(u => new SelectListItem
                {
                    Value = u.UserAccountId.ToString(),
                    Text = u.FullName
                }).ToList();
        }
    }
}