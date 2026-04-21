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
                TotalParticipants = _context.Participants.Count(),
                TotalEvents = _context.Events.Count(),
                TotalRegistrations = _context.Registrations.Count(),
                TotalGuests = _context.Guests.Count(),
                TotalPayments = _context.Payments.Count(),
                TotalQrPasses = _context.QrPasses.Count(),
                TotalDistributionLogs = _context.DistributionLogs.Count(),

                ApprovedRegistrations = _context.Registrations.Count(r => r.RegistrationStatus == "Approved" || r.RegistrationStatus == "Confirmed"),
                PendingRegistrations = _context.Registrations.Count(r => r.RegistrationStatus == "Pending"),
                RejectedRegistrations = _context.Registrations.Count(r => r.RegistrationStatus == "Rejected"),

                PaidPayments = _context.Payments.Count(p => p.PaymentStatus == "Paid"),
                PendingPayments = _context.Payments.Count(p => p.PaymentStatus == "Pending"),
                FailedPayments = _context.Payments.Count(p => p.PaymentStatus == "Failed")
            };

            return View(model);
        }
    }
}
