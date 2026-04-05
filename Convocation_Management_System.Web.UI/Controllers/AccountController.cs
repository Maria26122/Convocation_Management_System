using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly ConvocationDbContext _context;

        public AccountController(ConvocationDbContext context)
        {
            _context = context;
        }

        //--------------------------------------------
        // LOGIN PAGE
        //--------------------------------------------
        public IActionResult Login()
        {
            return View();
        }

        //--------------------------------------------
        // LOGIN POST
        //--------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is inactive.");
                return View(model);
            }

            bool isValidPassword = false;
            var passwordHasher = new PasswordHasher<UserAccount>();

            try
            {
                var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                isValidPassword = result != PasswordVerificationResult.Failed;
            }
            catch
            {
                // fallback for old plain text passwords
                if (user.PasswordHash == model.Password)
                {
                    isValidPassword = true;

                    // auto-convert old plain password to hashed password
                    user.PasswordHash = passwordHasher.HashPassword(user, model.Password);
                    await _context.SaveChangesAsync();
                }
            }

            if (!isValidPassword)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user.IsTwoFactorEnabled)
            {
                var otp = GenerateOtp();
                user.OtpCode = otp;
                user.OtpExpiryTime = DateTime.Now.AddMinutes(5);

                await _context.SaveChangesAsync();

                // Demo only
                TempData["OtpMessage"] = $"Demo OTP for {user.Email}: {otp}";

                return RedirectToAction(nameof(VerifyOtp), new { email = user.Email });
            }

            SignInUser(user);
            return RedirectByRole(user.Role?.RoleName);
        }

        //--------------------------------------------
        // TEMP HASH GENERATOR
        //--------------------------------------------
        public IActionResult GeneratePasswordHash()
        {
            var user = new UserAccount();
            var passwordHasher = new PasswordHasher<UserAccount>();

            string hashedPassword = passwordHasher.HashPassword(user, "123456");

            return Content(hashedPassword);
        }

        //--------------------------------------------
        // VERIFY OTP PAGE
        //--------------------------------------------
        public IActionResult VerifyOtp(string email)
        {
            var model = new VerifyOtpViewModel
            {
                Email = email
            };

            return View(model);
        }

        //--------------------------------------------
        // VERIFY OTP POST
        //--------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.UserAccounts
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

            if (DateTime.Now > user.OtpExpiryTime.Value)
            {
                ModelState.AddModelError("", "OTP expired. Please login again.");
                return View(model);
            }

            if (user.OtpCode != model.OtpCode)
            {
                ModelState.AddModelError("", "Invalid OTP.");
                return View(model);
            }

            user.OtpCode = null;
            user.OtpExpiryTime = null;

            await _context.SaveChangesAsync();

            SignInUser(user);
            return RedirectByRole(user.Role?.RoleName);
        }

        //--------------------------------------------
        // REGISTER PAGE
        //--------------------------------------------
        public IActionResult Register()
        {
            return View();
        }

        //--------------------------------------------
        // REGISTER POST
        //--------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailExists = await _context.UserAccounts
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(model);
            }

            var user = new UserAccount
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                CreatedAt = DateTime.Now,
                RoleId = 3, // Participant
                IsActive = true,
                IsTwoFactorEnabled = true
            };

            var passwordHasher = new PasswordHasher<UserAccount>();
            user.PasswordHash = passwordHasher.HashPassword(user, model.Password);

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            var participant = new Participant
            {
                UserAccountId = user.UserAccountId,
                StudentId = model.StudentId,
                Department = model.Department,
                Program = model.Program,
                Session = model.Session,
                CreatedAt = DateTime.Now
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful. Please login.";
            return RedirectToAction(nameof(Login));
        }

        //--------------------------------------------
        // LOGOUT
        //--------------------------------------------
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        //--------------------------------------------
        // HELPERS
        //--------------------------------------------
        private void SignInUser(UserAccount user)
        {
            HttpContext.Session.SetString("UserId", user.UserAccountId.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
        }

        private IActionResult RedirectByRole(string? roleName)
        {
            if (roleName == "Admin")
                return RedirectToAction("Index", "Admin");

            if (roleName == "Staff")
                return RedirectToAction("Verify", "QrPass");

            return RedirectToAction("Index", "Participant");
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}