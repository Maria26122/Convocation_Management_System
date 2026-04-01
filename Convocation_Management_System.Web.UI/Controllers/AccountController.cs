using Convocation.DataAccess;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly ConvocationDbContext _context;

        public AccountController(ConvocationDbContext context)
        {
            _context = context;
        }



        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserEmail") != null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == model.Email &&
                    u.PasswordHash == model.Password);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View(model);
            }

            HttpContext.Session.SetString("UserId", user.UserAccountId.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("RoleName", user.Role != null ? user.Role.RoleName : "User");

            return RedirectToAction("Index", "Admin");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}