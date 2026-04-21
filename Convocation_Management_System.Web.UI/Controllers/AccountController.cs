using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Helpers;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly ConvocationDbContext _context;

        public AccountController(ConvocationDbContext context)
        {
            _context = context;
        }
        //--------- REGISTER PAGE --------//
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

            bool emailExists = await _context.UserAccounts.AnyAsync(x => x.Email == vm.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(vm);
            }

            var participantRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == "student");

            if (participantRole == null)
            {
                ModelState.AddModelError("", "Participant role not found.");
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
                CreatedAt = DateTime.Now
            };

            _context.UserAccounts.Add(user);
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

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration completed successfully. Please login.";

            if (eventId.HasValue)
            {
                return RedirectToAction("Login", new
                {
                    returnUrl = Url.Action("Create", "Registration", new { eventId = eventId.Value })
                });
            }

            return RedirectToAction("Login");
        }

        // -------- LOGIN PAGE --------
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // -------- LOGIN POST --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(vm);

            var user = await _context.UserAccounts
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

            var roleName = (user.Role?.RoleName ?? "").Trim().ToLower();

            HttpContext.Session.SetString("UserId", user.UserAccountId.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");
            HttpContext.Session.SetString("FullName", user.FullName ?? "");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (roleName == "admin")
                return RedirectToAction("Index", "Admin");

            if (roleName == "eventmanager" || roleName == "staff")
                return RedirectToAction("Index", "Event");

            if (roleName == "student" || roleName == "participant")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.UserAccountId == user.UserAccountId);

                if (participant == null)
                {
                    ModelState.AddModelError("", "Participant profile not found for this account.");
                    return View(vm);
                }

                return RedirectToAction("Dashboard", "Participant");
            }

            ModelState.AddModelError("", "Role is invalid.");
            return View(vm);
        }


        // -------- LOGOUT --------
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        // -------- TEMPORARY HASH GENERATOR --------
        public IActionResult GenerateHash()
        {
            string hash = PasswordHelper.HashPassword("admin123");
            return Content(hash);
        }
    }
}