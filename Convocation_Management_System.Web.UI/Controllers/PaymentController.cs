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

        public IActionResult Test()
        {
            return Content("Controller working");
        }

        public async Task<IActionResult> Checkout(int id)
        {
            var registration = await _context.Registration
    .Include(r => r.Participant)
    .ThenInclude(p => p.UserAccount)
    .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            if (registration.RegistrationStatus == "Paid")
            {
                TempData["Success"] = "Payment already completed.";
                return RedirectToAction("Index", "Registration");
            }

            return View(registration);
        }

        public async Task<IActionResult> Pay(int id)
        {
            var registration = await _context.Registration
                .Include(r => r.Participant)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            if (registration.RegistrationStatus == "Paid")
            {
                TempData["Success"] = "Payment already completed.";
                return RedirectToAction("Index", "Registration");
            }

            decimal amount = 670;

            string tranId = "CNV-" +
                Guid.NewGuid().ToString("N")
                .Substring(0, 10)
                .ToUpper();

            registration.TransactionId = tranId;

            await _context.SaveChangesAsync();

            var payment = new Payment
            {
                RegistrationId = registration.RegistrationId,
                PaidAmount = amount,
                TransactionId = tranId,
                PaymentMethod = "SSLCommerz",
                PaymentStatus = "Pending",
                PaymentDate = DateTime.Now
            };

            _context.Payment.Add(payment);

            await _context.SaveChangesAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var postData = new Dictionary<string, string>
            {
                { "store_id", _config["SSLCommerz:StoreId"] ?? "" },
                { "store_passwd", _config["SSLCommerz:StorePassword"] ?? "" },

                { "total_amount", amount.ToString("0.00") },
                { "currency", "BDT" },
                { "tran_id", tranId },

                { "success_url", $"{baseUrl}/Payment/Success" },
                { "fail_url", $"{baseUrl}/Payment/Fail" },
                { "cancel_url", $"{baseUrl}/Payment/Cancel" },

                { "cus_name", registration.Participant.UserAccount?.FullName ?? "Student" },
{ "cus_email", registration.Participant.UserAccount?.Email ?? "student@gmail.com" },
{ "cus_phone", registration.Participant.UserAccount?.Phone ?? "01700000000" },

                { "cus_add1", "Bangladesh" },
                { "cus_city", "Dhaka" },
                { "cus_country", "Bangladesh" },

                { "shipping_method", "NO" },

                { "product_name", "Convocation Registration Fee" },
                { "product_category", "Education" },
                { "product_profile", "general" }
            };

            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsync(
                _config["SSLCommerz:GatewayUrl"],
                new FormUrlEncodedContent(postData));

            var json = await response.Content.ReadAsStringAsync();

            using var result = JsonDocument.Parse(json);

            if (result.RootElement.TryGetProperty(
                "GatewayPageURL",
                out var gatewayUrl))
            {
                var url = gatewayUrl.GetString();

                if (!string.IsNullOrWhiteSpace(url))
                {
                    return Redirect(url);
                }
            }

            TempData["Error"] =
                "SSLCommerz gateway connection failed.";

            return RedirectToAction("Index", "Registration");
        }

        [HttpPost]
        public async Task<IActionResult> Success()
        {
            string tranId = Request.Form["tran_id"];

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p =>
                    p.TransactionId == tranId);

            if (payment != null)
            {
                payment.PaymentStatus = "Paid";
                payment.PaymentDate = DateTime.Now;

                var registration = await _context.Registration
                    .FirstOrDefaultAsync(r =>
                        r.RegistrationId ==
                        payment.RegistrationId);

                if (registration != null)
                {
                    registration.RegistrationStatus = "Paid";
                }

                await _context.SaveChangesAsync();
            }

            TempData["Success"] =
                "Payment completed successfully.";

            return RedirectToAction("Index", "Registration");
        }

        [HttpPost]
        public async Task<IActionResult> Fail()
        {
            string tranId = Request.Form["tran_id"];

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p =>
                    p.TransactionId == tranId);

            if (payment != null)
            {
                payment.PaymentStatus = "Failed";

                await _context.SaveChangesAsync();
            }

            TempData["Error"] =
                "Payment failed.";

            return RedirectToAction("Index", "Registration");
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            TempData["Error"] =
                "Payment cancelled.";

            return RedirectToAction("Index", "Registration");
        }
    }
}