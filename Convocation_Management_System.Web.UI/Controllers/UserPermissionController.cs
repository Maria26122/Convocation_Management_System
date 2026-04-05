using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class UserPermissionController : Controller
    {
        private readonly ConvocationDbContext _context;

        public UserPermissionController(ConvocationDbContext context)
        {
            _context = context;
        }

        private async Task LoadDropdownsAsync(object? selectedUser = null, object? selectedPermission = null)
        {
            ViewBag.UserAccountId = new SelectList(await _context.UserAccounts.OrderBy(u => u.FullName).ToListAsync(), "UserAccountId", "FullName", selectedUser);
            ViewBag.PermissionId = new SelectList(await _context.Permissions.OrderBy(p => p.PermissionName).ToListAsync(), "PermissionId", "PermissionName", selectedPermission);
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            var items = await _context.UserPermissions
                .Include(up => up.UserAccount)
                .Include(up => up.Permission)
                .OrderBy(up => up.UserAccount!.FullName)
                .ThenBy(up => up.Permission!.PermissionName)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.UserPermissions
                .Include(up => up.UserAccount)
                .Include(up => up.Permission)
                .FirstOrDefaultAsync(up => up.UserPermissionId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new UserPermission());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserPermission userPermission)
        {
            if (await _context.UserPermissions.AnyAsync(up => up.UserAccountId == userPermission.UserAccountId && up.PermissionId == userPermission.PermissionId))
            {
                ModelState.AddModelError(string.Empty, "This user-permission mapping already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(userPermission.UserAccountId, userPermission.PermissionId);
                return View(userPermission);
            }

            _context.UserPermissions.Add(userPermission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "User permission created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.UserPermissions.FindAsync(id);
            if (item == null) return NotFound();
            await LoadDropdownsAsync(item.UserAccountId, item.PermissionId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserPermission userPermission)
        {
            if (id != userPermission.UserPermissionId) return NotFound();

            if (await _context.UserPermissions.AnyAsync(up => up.UserAccountId == userPermission.UserAccountId && up.PermissionId == userPermission.PermissionId && up.UserPermissionId != userPermission.UserPermissionId))
            {
                ModelState.AddModelError(string.Empty, "This user-permission mapping already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(userPermission.UserAccountId, userPermission.PermissionId);
                return View(userPermission);
            }

            _context.Update(userPermission);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "User permission updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.UserPermissions
                .Include(up => up.UserAccount)
                .Include(up => up.Permission)
                .FirstOrDefaultAsync(up => up.UserPermissionId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.UserPermissions.FindAsync(id);
            if (item != null)
            {
                _context.UserPermissions.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User permission deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
