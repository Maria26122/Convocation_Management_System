using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Helpers;
using Convocation_Management_System.Web.UI.Models;
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
        public IActionResult Register(int? eventId = null, string? returnUrl = null)
        {
            ViewBag.EventId = eventId;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm, int? eventId = null, string? returnUrl = null)
        {
            ViewBag.EventId = eventId;
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(vm);

            bool emailExists = await _context.UserAccount.AnyAsync(x => x.Email == vm.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(vm);
            }

            bool studentIdExists = await _context.Participant.AnyAsync(p => p.StudentId == vm.StudentId);
            if (studentIdExists)
            {
                ModelState.AddModelError("StudentId", "Student ID already exists.");
                return View(vm);
            }

            var participantRole = await _context.Role
                .FirstOrDefaultAsync(r =>
                    r.RoleName.ToLower() == "student" ||
                    r.RoleName.ToLower() == "participant");

            if (participantRole == null)
            {
                ModelState.AddModelError("", "Student role not found in database.");
                return View(vm);
            }

            var user = new UserAccount
            {
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                PasswordHash = PasswordHelper.HashPassword(vm.Password),
                RoleId = participantRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                IsTwoFactorEnabled = false
            };

            _context.UserAccount.Add(user);
            await _context.SaveChangesAsync();

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

            user.Role = participantRole;
            await SignInUserAsync(user);

            if (eventId.HasValue && eventId.Value > 0)
            {
                return RedirectToAction("Create", "Registration", new { eventId = eventId.Value });
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard", "Participant");
        }

        // =========================
        // LOGIN
        // =========================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(vm);

            var user = await _context.UserAccount
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == vm.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(vm);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is inactive.");
                return View(vm);
            }

            bool passwordOk = PasswordHelper.VerifyPassword(vm.Password, user.PasswordHash);
            if (!passwordOk)
            {
                ModelState.AddModelError("", "Wrong password.");
                return View(vm);
            }

            await SignInUserAsync(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectByRole(user.Role?.RoleName ?? "");
        }

        // =========================
        // VERIFY OTP
        // =========================
        [HttpGet]
        public IActionResult VerifyOtp(string email, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            ViewBag.ReturnUrl = returnUrl;

            return View(new VerifyOtpViewModel
            {
                Email = email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.UserAccount
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.OtpCode) || user.OtpExpiryTime == null)
            {
                ModelState.AddModelError("", "OTP not found. Please login again.");
                return View(model);
            }

            if (user.OtpExpiryTime < DateTime.Now)
            {
                ModelState.AddModelError("", "OTP expired. Please login again.");
                return View(model);
            }

            if ((user.OtpCode ?? "").Trim() != (model.OtpCode ?? "").Trim())
            {
                ModelState.AddModelError("", "Invalid OTP.");
                return View(model);
            }

            user.OtpCode = null;
            user.OtpExpiryTime = null;
            await _context.SaveChangesAsync();

            await SignInUserAsync(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectByRole(user.Role?.RoleName ?? "");
        }

        // =========================
        // CHANGE PASSWORD
        // =========================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
                return View(model);

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login");

            var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.UserAccountId == userId);
            if (user == null)
                return RedirectToAction("Login");

            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("ChangePassword");
        }

        // =========================
        // LOGOUT
        // =========================
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // HELPERS
        // =========================
        private async Task SignInUserAsync(UserAccount user)
        {
            var roleName = (user.Role?.RoleName ?? "").Trim();

            HttpContext.Session.SetString("UserId", user.UserAccountId.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("Role", roleName);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserAccountId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, roleName)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

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

        private IActionResult RedirectByRole(string roleName)
        {
            var normalizedRole = (roleName ?? "").Trim().ToLower();

            if (normalizedRole == "admin")
                return RedirectToAction("Index", "Admin");

            if (normalizedRole == "eventmanager" || normalizedRole == "staff")
                return RedirectToAction("Index", "Event");

            if (normalizedRole == "student" || normalizedRole == "participant")
                return RedirectToAction("Dashboard", "Participant");

            return RedirectToAction("Login");
        }

        public IActionResult GenerateHash()
        {
            string hash = PasswordHelper.HashPassword("admin123");
            return Content(hash);
        }
    }
}