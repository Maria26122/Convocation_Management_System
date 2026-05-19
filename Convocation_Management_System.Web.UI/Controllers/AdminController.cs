using Convocation.DataAccess;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class AdminController : Controller
    {
        private readonly ConvocationDbContext _context;

        public AdminController(ConvocationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "admin,organizer")]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role")?.ToLower() != "admin")
            {
                return RedirectToAction("Login", "Account");
            }
            var model = new AdminDashboardViewModel
            {
                TotalParticipant = _context.Participant.Count(),
                TotalEvent = _context.Event.Count(),
                TotalRegistration = _context.Registration.Count(),
                TotalGuest = _context.Guest.Count(),
                TotalPayment = _context.Payment.Count(),
                TotalQrPass = _context.QrPass.Count(),
                TotalDistributionLog = _context.DistributionLog.Count(),
                ApprovedRegistration = _context.Registration.Count(r => r.RegistrationStatus == "Approved" || r.RegistrationStatus == "Confirmed"),
                PendingRegistration = _context.Registration.Count(r => r.RegistrationStatus == "Pending"),
                RejectedRegistration = _context.Registration.Count(r => r.RegistrationStatus == "Rejected"),

                PaidPayment = _context.Payment.Count(p => p.PaymentStatus == "Paid"),
                PendingPayment = _context.Payment.Count(p => p.PaymentStatus == "Pending"),
                FailedPayment = _context.Payment.Count(p => p.PaymentStatus == "Failed")
            };

            return View(model);
        }
    }
}
