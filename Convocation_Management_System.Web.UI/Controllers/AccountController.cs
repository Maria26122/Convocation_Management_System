using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Convocation_Management_System.Web.UI.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly ConvocationDbContext _context;

        public AccountController(ConvocationDbContext context)
        {
            _context = context;
        }

        // =========================
        // REGISTER
        // =========================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var emailExists = await _context.UserAccount
                .AnyAsync(x => x.Email == vm.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(vm);
            }

            var role = await _context.Role
                .FirstOrDefaultAsync(r =>
                    r.RoleName.ToLower() == "student" ||
                    r.RoleName.ToLower() == "participant");

            if (role == null)
            {
                ModelState.AddModelError("", "Role not found");
                return View(vm);
            }

            // CREATE USER
            var user = new UserAccount
            {
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                PasswordHash = PasswordHelper.HashPassword(vm.Password),
                RoleId = role.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.UserAccount.Add(user);
            await _context.SaveChangesAsync();

            // CREATE PARTICIPANT
            var participant = new Participant
            {
                UserAccountId = user.UserAccountId,
                StudentId = vm.StudentId,
                Department = vm.Department,
                Program = vm.Program,
                Session = vm.Session,
                IsEligible = true,
                CreatedAt = DateTime.Now
            };

            _context.Participant.Add(participant);
            await _context.SaveChangesAsync();

            // LOGIN USER
            await SignInUserAsync(user, role.RoleName);

            return RedirectToAction("Dashboard", "Participant");
        }



        // =========================
        // LOGIN (FIXED)
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var hashedPassword = PasswordHelper.HashPassword(password);

            var user = await _context.UserAccount
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.PasswordHash == hashedPassword);

            if (user == null)
            {
                TempData["Error"] = "Invalid login";
                return View();
            }

            if (!user.IsActive)
            {
                TempData["Error"] = "Account inactive";
                return View();
            }

            var roleName = (user.Role?.RoleName ?? "").Trim().ToLower();

            // SESSION (IMPORTANT)
            HttpContext.Session.SetString("UserId", user.UserAccountId.ToString());
            HttpContext.Session.SetString("Role", roleName);

            await SignInUserAsync(user, roleName);

            if (roleName == "admin")
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Dashboard", "Participant");
        }

        // =========================
        // SIGN IN HELPER (FIXED)
        // =========================
        private async Task SignInUserAsync(UserAccount user, string roleName)
        {
            roleName = (roleName ?? "").Trim().ToLower();

            HttpContext.Session.SetString("UserId", user.UserAccountId.ToString());
            HttpContext.Session.SetString("Role", roleName);
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("FullName", user.FullName ?? "");

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserAccountId.ToString()),
        new Claim(ClaimTypes.Name, user.FullName ?? ""),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, roleName)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                });
        }
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
    }
}