using Convocation.DataAccess;
using Convocation_Management_System.Web.UI.Models;
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

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            var model = new AdminDashboardViewModel
            {
                TotalParticipants = _context.Participant.Count(),
                TotalEvents = _context.Event.Count(),
                TotalRegistrations = _context.Registration.Count(),
                TotalGuests = _context.Guest.Count(),
                TotalPayments = _context.Payment.Count(),
                TotalQrPasses = _context.QrPass.Count(),
                TotalDistributionLogs = _context.DistributionLog.Count(),
                ApprovedRegistrations = _context.Registration.Count(r => r.RegistrationStatus == "Approved" || r.RegistrationStatus == "Confirmed"),
                PendingRegistrations = _context.Registration.Count(r => r.RegistrationStatus == "Pending"),
                RejectedRegistrations = _context.Registration.Count(r => r.RegistrationStatus == "Rejected"),

                PaidPayments = _context.Payment.Count(p => p.PaymentStatus == "Paid"),
                PendingPayments = _context.Payment.Count(p => p.PaymentStatus == "Pending"),
                FailedPayments = _context.Payment.Count(p => p.PaymentStatus == "Failed")
            };

            return View(model);
        }
    }
}
