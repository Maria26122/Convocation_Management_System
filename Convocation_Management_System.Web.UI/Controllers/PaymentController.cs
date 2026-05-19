using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ConvocationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentController(
            ConvocationDbContext context,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // =========================
        // TEST
        // =========================
        public IActionResult Test()
        {
            return Content("Payment Controller Working");
        }

        // =========================
        // CHECKOUT PAGE
        // =========================
        public async Task<IActionResult> Checkout(int id)
        {
            var registration = await _context.Registration
                .Include(r => r.Participant)
                .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            return View(registration);
        }

        // =========================
        // PAY NOW (SSLCommerz START)
        // =========================
        public async Task<IActionResult> PayNow(int registrationId)
        {
            var registration = await _context.Registration
                .Include(r => r.Participant)
                .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration == null)
                return NotFound();

            decimal amount = registration.TotalAmount;

            string tranId = "CNV-" + Guid.NewGuid().ToString("N")[..10].ToUpper();

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.RegistrationId == registrationId);

            if (payment == null)
            {
                payment = new Payment
                {
                    RegistrationId = registrationId,
                    PaidAmount = amount,
                    TransactionId = tranId,
                    PaymentMethod = "SSLCommerz",
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Payment.Add(payment);
            }
            else
            {
                payment.TransactionId = tranId;
                payment.PaymentStatus = "Pending";
                payment.PaidAmount = amount;
            }

            await _context.SaveChangesAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var postData = new Dictionary<string, string>
            {
                ["store_id"] = _config["SSLCommerz:StoreId"],
                ["store_passwd"] = _config["SSLCommerz:StorePassword"],
                ["total_amount"] = amount.ToString("0.00"),
                ["currency"] = "BDT",
                ["tran_id"] = tranId,

                ["success_url"] = $"{baseUrl}/Payment/Success",
                ["fail_url"] = $"{baseUrl}/Payment/Fail",
                ["cancel_url"] = $"{baseUrl}/Payment/Cancel",

                ["cus_name"] = registration.Participant?.UserAccount?.FullName ?? "Student",
                ["cus_email"] = registration.Participant?.UserAccount?.Email ?? "test@test.com",
                ["cus_phone"] = registration.Participant?.UserAccount?.Phone ?? "01700000000",

                ["shipping_method"] = "NO",
                ["product_name"] = "Convocation Fee",
                ["product_category"] = "Education",
                ["product_profile"] = "general"
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                _config["SSLCommerz:GatewayUrl"],
                new FormUrlEncodedContent(postData));

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            if (result.RootElement.TryGetProperty("GatewayPageURL", out var url))
            {
                var gateway = url.GetString();
                return Redirect(gateway);
            }

            TempData["Error"] = "Payment gateway failed.";
            return RedirectToAction("MyRegistration", "Participant");
        }

        // =========================
        // SUCCESS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Success(string tran_id)
        {
            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.TransactionId == tran_id);

            if (payment != null)
            {
                payment.PaymentStatus = "Paid";
                payment.VerifiedAt = DateTime.Now;

                var registration = await _context.Registration
                    .FirstOrDefaultAsync(r => r.RegistrationId == payment.RegistrationId);

                if (registration != null)
                {
                    registration.RegistrationStatus = "Confirmed";
                }

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Payment successful!";
            return RedirectToAction("MyPayment", "Participant");
        }

        // =========================
        // FAIL
        // =========================
        [HttpGet]
        public async Task<IActionResult> Fail(string tran_id)
        {
            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.TransactionId == tran_id);

            if (payment != null)
            {
                payment.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();
            }

            TempData["Error"] = "Payment failed.";
            return RedirectToAction("MyPayment", "Participant");
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment cancelled.";
            return RedirectToAction("MyPayment", "Participant");
        }

        // =========================
        // CANCEL
        // =========================
        [HttpGet]
        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment cancelled.";
            return RedirectToAction("MyPayment", "Participant");
        }
    }
}