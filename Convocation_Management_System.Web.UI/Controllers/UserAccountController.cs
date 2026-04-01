using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class UserAccountController : Controller
    {
        private readonly ConvocationDbContext _context;

        public UserAccountController(ConvocationDbContext context)
        {
            _context = context;
        }

        private async Task LoadRolesAsync(object? selectedRole = null)
        {
            ViewBag.RoleId = new SelectList(await _context.Roles.OrderBy(r => r.RoleName).ToListAsync(), "RoleId", "RoleName", selectedRole);
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.UserAccounts
                .Include(u => u.Role)
                .OrderByDescending(u => u.UserAccountId)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .Include(u => u.Participant)
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.UserAccountId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        public async Task<IActionResult> Create()
        {
            await LoadRolesAsync();
            return View(new UserAccount { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAccount userAccount)
        {
            if (await _context.UserAccounts.AnyAsync(u => u.Email == userAccount.Email))
            {
                ModelState.AddModelError("Email", "This email already exists.");
            }

            if (userAccount.RoleId <= 0)
            {
                ModelState.AddModelError("RoleId", "Please select a role.");
            }

            if (!ModelState.IsValid)
            {
                await LoadRolesAsync(userAccount.RoleId);
                return View(userAccount);
            }

            userAccount.CreatedAt = DateTime.Now;
            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            await LoadRolesAsync(user.RoleId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserAccount userAccount)
        {
            if (id != userAccount.UserAccountId) return NotFound();

            if (await _context.UserAccounts.AnyAsync(u => u.Email == userAccount.Email && u.UserAccountId != userAccount.UserAccountId))
            {
                ModelState.AddModelError("Email", "This email already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadRolesAsync(userAccount.RoleId);
                return View(userAccount);
            }

            try
            {
                _context.Update(userAccount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.UserAccounts.AnyAsync(u => u.UserAccountId == id))
                    return NotFound();
                throw;
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserAccountId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return RedirectToAction(nameof(Index));

            bool hasParticipant = await _context.Participants.AnyAsync(p => p.UserAccountId == id);
            bool hasDistributionLog = await _context.DistributionLogs.AnyAsync(d => d.UserAccountId == id);
            bool hasUserPermission = await _context.UserPermissions.AnyAsync(up => up.UserAccountId == id);

            if (hasParticipant || hasDistributionLog || hasUserPermission)
            {
                TempData["ErrorMessage"] = "This user cannot be deleted because related records already exist.";
                return RedirectToAction(nameof(Index));
            }

            _context.UserAccounts.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
