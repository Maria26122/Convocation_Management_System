using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
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

        // LIST
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.StaffTask
                .Include(t => t.UserAccount)
                .OrderByDescending(t => t.AssignedAt)
                .ToListAsync();

            return View(tasks);
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            var model = new StaffTaskViewModel
            {
                Status = "Pending",
                AssignedAt = DateTime.Now,

                Users = _context.UserAccount
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserAccountId.ToString(),
                        Text = u.FullName
                    }).ToList()
            };

            return View(model);
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Users = _context.UserAccount
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserAccountId.ToString(),
                        Text = u.FullName
                    }).ToList();

                return View(model);
            }

            var task = new StaffTask
            {
                UserAccountId = model.UserAccountId,
                TaskTitle = model.TaskTitle,
                Description = model.Description,
                Status = "Pending",
                AssignedAt = DateTime.Now
            };

            _context.StaffTask.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // EDIT (GET)
        public IActionResult Edit(int id)
        {
            var task = _context.StaffTask.FirstOrDefault(x => x.StaffTaskId == id);

            if (task == null) return NotFound();

            var model = new StaffTaskViewModel
            {
                StaffTaskId = task.StaffTaskId,
                UserAccountId = task.UserAccountId,
                TaskTitle = task.TaskTitle,
                Description = task.Description,
                Status = task.Status,
                Remarks = task.Remarks,
                AssignedAt = task.AssignedAt,
                CompletedAt = task.CompletedAt,

                Users = _context.UserAccount
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserAccountId.ToString(),
                        Text = u.FullName
                    }).ToList()
            };

            return View(model);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StaffTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Users = _context.UserAccount
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserAccountId.ToString(),
                        Text = u.FullName
                    }).ToList();

                return View(model);
            }

            var task = await _context.StaffTask
                .FirstOrDefaultAsync(x => x.StaffTaskId == model.StaffTaskId);

            if (task == null) return NotFound();

            task.UserAccountId = model.UserAccountId;
            task.TaskTitle = model.TaskTitle;
            task.Description = model.Description;
            task.Status = model.Status;
            task.Remarks = model.Remarks;

            if (model.Status == "Completed" && task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.StaffTask
                .Include(t => t.UserAccount)
                .FirstOrDefaultAsync(t => t.StaffTaskId == id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
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

        // HELPER
        private void LoadUsers()
        {
            ViewBag.Users = _context.UserAccount
                .Select(u => new
                {
                    u.UserAccountId,
                    FullName = u.FullName ?? "Unknown User"
                })
                .ToList();
        }
    }
}