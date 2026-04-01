using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class RolePermissionController : Controller
    {
        private readonly ConvocationDbContext _context;

        public RolePermissionController(ConvocationDbContext context)
        {
            _context = context;
        }

        private async Task LoadDropdownsAsync(object? selectedRole = null, object? selectedPermission = null)
        {
            ViewBag.RoleId = new SelectList(await _context.Roles.OrderBy(r => r.RoleName).ToListAsync(), "RoleId", "RoleName", selectedRole);
            ViewBag.PermissionId = new SelectList(await _context.Permissions.OrderBy(p => p.PermissionName).ToListAsync(), "PermissionId", "PermissionName", selectedPermission);
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .OrderBy(rp => rp.Role!.RoleName)
                .ThenBy(rp => rp.Permission!.PermissionName)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.RolePermissionId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new RolePermission());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RolePermission rolePermission)
        {
            if (await _context.RolePermissions.AnyAsync(rp => rp.RoleId == rolePermission.RoleId && rp.PermissionId == rolePermission.PermissionId))
            {
                ModelState.AddModelError(string.Empty, "This role-permission mapping already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(rolePermission.RoleId, rolePermission.PermissionId);
                return View(rolePermission);
            }

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Role permission created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.RolePermissions.FindAsync(id);
            if (item == null) return NotFound();
            await LoadDropdownsAsync(item.RoleId, item.PermissionId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RolePermission rolePermission)
        {
            if (id != rolePermission.RolePermissionId) return NotFound();

            if (await _context.RolePermissions.AnyAsync(rp => rp.RoleId == rolePermission.RoleId && rp.PermissionId == rolePermission.PermissionId && rp.RolePermissionId != rolePermission.RolePermissionId))
            {
                ModelState.AddModelError(string.Empty, "This role-permission mapping already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(rolePermission.RoleId, rolePermission.PermissionId);
                return View(rolePermission);
            }

            _context.Update(rolePermission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Role permission updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.RolePermissionId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.RolePermissions.FindAsync(id);
            if (item != null)
            {
                _context.RolePermissions.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Role permission deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
