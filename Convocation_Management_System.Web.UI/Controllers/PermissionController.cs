using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class PermissionController : Controller
    {
        private readonly ConvocationDbContext _context;

        public PermissionController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.PermissionName)
                .ToListAsync();
            return View(permissions);
        }

        public IActionResult Create()
        {
            return View(new Permission());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Permission permission)
        {
            if (await _context.Permissions.AnyAsync(p => p.PermissionName == permission.PermissionName))
            {
                ModelState.AddModelError("PermissionName", "This permission already exists.");
            }

            if (!ModelState.IsValid) return View(permission);

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Permission created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                    .ThenInclude(rp => rp.Role)
                .Include(p => p.UserPermissions)
                    .ThenInclude(up => up.UserAccount)
                .FirstOrDefaultAsync(p => p.PermissionId == id);

            if (permission == null) return NotFound();
            return View(permission);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();
            return View(permission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Permission permission)
        {
            if (id != permission.PermissionId) return NotFound();

            if (await _context.Permissions.AnyAsync(p => p.PermissionName == permission.PermissionName && p.PermissionId != permission.PermissionId))
            {
                ModelState.AddModelError("PermissionName", "This permission already exists.");
            }

            if (!ModelState.IsValid) return View(permission);

            _context.Update(permission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Permission updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionId == id);
            if (permission == null) return NotFound();
            return View(permission);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return RedirectToAction(nameof(Index));

            bool inRolePermission = await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == id);
            bool inUserPermission = await _context.UserPermissions.AnyAsync(up => up.PermissionId == id);
            if (inRolePermission || inUserPermission)
            {
                TempData["ErrorMessage"] = "This permission cannot be deleted because it is already assigned.";
                return RedirectToAction(nameof(Index));
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Permission deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
