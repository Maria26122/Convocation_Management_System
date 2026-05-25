using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Services;
using Convocation_Management_System.Web.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Convocation_Management_System.Web.UI.Controllers
{
    [Authorize(Roles = "admin")]
    public class PaymentController : Controller
    {
        private readonly ConvocationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QrGeneratorService _qrService;
        private readonly EmailService _emailService;

        public PaymentController(
            ConvocationDbContext context,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            EmailService emailService,
            QrGeneratorService qrService)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
            _qrService = qrService;
        }

        // =========================================
        // ADMIN PAYMENT LIST
        // =========================================
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

        // =========================================
        // START PAYMENT
        // =========================================
        [AllowAnonymous]
        public async Task<IActionResult> PayNow(int registrationId)
        {
            var registration = await _context.Registration
                .Include(r => r.Participant)
                    .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration == null)
                return NotFound();

            var amount = registration.TotalAmount;

            var tranId = "CNV-" + Guid.NewGuid()
                .ToString("N")[..10]
                .ToUpper();

            registration.TransactionId = tranId;

            var payment = new Payment
            {
                RegistrationId = registration.RegistrationId,
                PaidAmount = amount,
                TransactionId = tranId,
                PaymentMethod = "SSLCommerz",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Payment.Add(payment);

            await _context.SaveChangesAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var postData = new Dictionary<string, string>
            {
                { "store_id", _config["SSLCommerz:StoreId"] },
                { "store_passwd", _config["SSLCommerz:StorePassword"] },

                { "total_amount", amount.ToString("0.00") },
                { "currency", "BDT" },
                { "tran_id", tranId },

                { "success_url", $"{baseUrl}/Payment/Success" },
                { "fail_url", $"{baseUrl}/Payment/Fail" },
                { "cancel_url", $"{baseUrl}/Payment/Cancel" },

                { "cus_name", registration.Participant.UserAccount.FullName },
                { "cus_email", registration.Participant.UserAccount.Email },
                { "cus_phone", registration.Participant.UserAccount.Phone },

                { "cus_add1", "Bangladesh" },
                { "cus_city", "Dhaka" },
                { "cus_country", "Bangladesh" },

                { "shipping_method", "NO" },
                { "product_name", "Convocation Fee" },
                { "product_category", "Education" },
                { "product_profile", "general" }
            };

            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsync(
                _config["SSLCommerz:GatewayUrl"],
                new FormUrlEncodedContent(postData));

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonDocument.Parse(json);

            if (result.RootElement.TryGetProperty("GatewayPageURL", out var url))
            {
                return Redirect(url.GetString());
            }

            TempData["Error"] = "Payment failed to initialize.";

            return RedirectToAction("MyRegistration", "Participant");
        }

        // =========================================
        // PAYMENT SUCCESS
        // =========================================
        [AllowAnonymous]
        [HttpPost]
        [HttpGet, HttpPost]
        public async Task<IActionResult> Success()
        {
            string tranId = Request.Form["tran_id"].FirstOrDefault()
                           ?? Request.Query["tran_id"].FirstOrDefault();

            var payment = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.TransactionId == tranId);

            if (payment == null)
                return RedirectToAction("Index", "Home");

            payment.PaymentStatus = "Paid";
            payment.CreatedAt = DateTime.Now;

            var registration = payment.Registration;

            if (registration == null)
                return RedirectToAction("Index", "Home");

            registration.RegistrationStatus = "Paid";

            await _context.SaveChangesAsync();

            // GENERATE QR
            var qrText = $"REG-{registration.RegistrationId}-USER-{registration.ParticipantId}";

            var qrPath = _qrService.GenerateQr(qrText);

            if (registration.Payment.PaymentStatus == "Paid")
            {
                var qrCodeText = SimpleQrBuilder.Build(
                    registration.RegistrationId,
                    registration.EventId,
                    registration.Participant.UserAccountId
                );

                var qrImagePath = _qrService.GenerateQr(qrCodeText);

                var qr = new QrPass
                {
                    RegistrationId = registration.RegistrationId,
                    QrCodeText = qrCodeText,
                    QrImagePath = qrImagePath,
                    CreatedAt = DateTime.Now
                };

                _context.QrPass.Add(qr);
                await _context.SaveChangesAsync();
            }
            // EMAIL
            var email = registration.Participant.UserAccount.Email;
            var name = registration.Participant.UserAccount.FullName;

            await _emailService.SendEmailAsync(
                email,
                "Convocation Payment & QR Pass",
                $"<p>Dear {name}, payment successful.</p>",
                qrPath);

            TempData["Success"] = "Payment successful! QR sent to your email.";

            return RedirectToAction("MyQrPass", "Participant",
     new { registrationId = registration.RegistrationId });
        }

        // =========================================
        // PAYMENT FAIL
        // =========================================
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Fail()
        {
            var tranId = Request.Form["tran_id"].ToString();

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.TransactionId == tranId);

            if (payment != null)
            {
                payment.PaymentStatus = "Failed";

                await _context.SaveChangesAsync();
            }

            TempData["Error"] = "Payment failed.";

            return RedirectToAction("MyRegistration", "Participant");
        }

        // =========================================
        // PAYMENT CANCEL
        // =========================================
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment cancelled.";

            return RedirectToAction("MyRegistration", "Participant");
        }

        // =========================================
        // ADMIN APPROVE PAYMENT
        // =========================================
        public async Task<IActionResult> Approve(int id)
        {
            var payment = await _context.Payment.FindAsync(id);

            if (payment == null)
                return NotFound();

            payment.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // ADMIN REJECT PAYMENT
        // =========================================
        public async Task<IActionResult> Reject(int id)
        {
            var payment = await _context.Payment.FindAsync(id);

            if (payment == null)
                return NotFound();

            payment.PaymentStatus = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}