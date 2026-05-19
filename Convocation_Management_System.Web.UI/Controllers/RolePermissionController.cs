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

        // =========================
        // LOAD DROPDOWNS
        // =========================
        private async Task LoadDropdownsAsync(object? selectedRole = null, object? selectedPermission = null)
        {
            ViewBag.RoleId = new SelectList(
                await _context.Role.OrderBy(r => r.RoleName).ToListAsync(),
                "RoleId",
                "RoleName",
                selectedRole
            );

            ViewBag.PermissionId = new SelectList(
                await _context.Permission.OrderBy(p => p.PermissionName).ToListAsync(),
                "PermissionId",
                "PermissionName",
                selectedPermission
            );
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null ||
                HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var items = await _context.RolePermission
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .OrderBy(rp => rp.Role.RoleName)
                .ThenBy(rp => rp.Permission.PermissionName)
                .ToListAsync();

            return View(items);
        }

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> Details(int roleId, int permissionId)
        {
            var item = await _context.RolePermission
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp =>
                    rp.RoleId == roleId &&
                    rp.PermissionId == permissionId);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // =========================
        // CREATE (GET)
        // =========================
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RolePermission rolePermission)
        {
            var exists = await _context.RolePermission.AnyAsync(rp =>
                rp.RoleId == rolePermission.RoleId &&
                rp.PermissionId == rolePermission.PermissionId);

            if (exists)
            {
                ModelState.AddModelError("", "This role-permission mapping already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(rolePermission.RoleId, rolePermission.PermissionId);
                return View(rolePermission);
            }

            _context.RolePermission.Add(rolePermission);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Role permission created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        public async Task<IActionResult> Edit(int roleId, int permissionId)
        {
            var item = await _context.RolePermission.FirstOrDefaultAsync(rp =>
                rp.RoleId == roleId &&
                rp.PermissionId == permissionId);

            if (item == null)
                return NotFound();

            await LoadDropdownsAsync(item.RoleId, item.PermissionId);
            return View(item);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int roleId, int permissionId, RolePermission rolePermission)
        {
            var exists = await _context.RolePermission.AnyAsync(rp =>
                rp.RoleId == rolePermission.RoleId &&
                rp.PermissionId == rolePermission.PermissionId &&
                !(rp.RoleId == roleId && rp.PermissionId == permissionId));

            if (exists)
            {
                ModelState.AddModelError("", "This role-permission mapping already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(rolePermission.RoleId, rolePermission.PermissionId);
                return View(rolePermission);
            }

            var old = await _context.RolePermission.FirstOrDefaultAsync(rp =>
                rp.RoleId == roleId &&
                rp.PermissionId == permissionId);

            if (old == null)
                return NotFound();

            _context.RolePermission.Remove(old);
            _context.RolePermission.Add(rolePermission);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Role permission updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE (GET)
        // =========================
        public async Task<IActionResult> Delete(int roleId, int permissionId)
        {
            var item = await _context.RolePermission
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp =>
                    rp.RoleId == roleId &&
                    rp.PermissionId == permissionId);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // =========================
        // DELETE (POST)
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int roleId, int permissionId)
        {
            var item = await _context.RolePermission.FirstOrDefaultAsync(rp =>
                rp.RoleId == roleId &&
                rp.PermissionId == permissionId);

            if (item != null)
            {
                _context.RolePermission.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Role permission deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}