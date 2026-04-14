using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ConvocationDbContext _context;
        private readonly SSLCommerzSettings _settings;

        public PaymentController(
            ConvocationDbContext context,
            IOptions<SSLCommerzSettings> settings)
        {
            _context = context;
            _settings = settings.Value;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartConvocationPayment(int registrationId)
        {
            var registration = await _context.Registrations
                .Include(r => r.Participant)
                .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration == null)
                return NotFound();

            var participant = registration.Participant;
            if (participant == null || participant.UserAccount == null)
                return BadRequest("Participant data not found.");

            decimal amount = 3000; // example convocation fee
            string tranId = "CNV-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var payment = new Payment 
            {
                Registration=registration,

                RegistrationId = participant.ParticipantId, PaymentId = registration.RegistrationId, PaidAmount = amount, PaymentStatus = "Pending", TransactionId = tranId, PaymentDate = DateTime.Now };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var payload = new SSLCommerzInitRequest
            {
                total_amount = amount,
                tran_id = tranId,
                success_url = _settings.SuccessUrl,
                fail_url = _settings.FailUrl,
                cancel_url = _settings.CancelUrl,
                ipn_url = _settings.IpnUrl,
                product_name = "Convocation Registration Fee",
                cus_name = participant.UserAccount.FullName ?? "Student",
                cus_email = participant.UserAccount.Email ?? "",
                cus_phone = participant.UserAccount.Phone ?? "01700000000",
                value_a = registration.RegistrationId.ToString(),
                value_b = participant.ParticipantId.ToString()
            };

            using var client = new HttpClient();
            var dict = new Dictionary<string, string>
            {
                ["store_id"] = _settings.StoreId,
                ["store_passwd"] = _settings.StorePassword,
                ["total_amount"] = payload.total_amount.ToString("0.00"),
                ["currency"] = payload.currency,
                ["tran_id"] = payload.tran_id,
                ["success_url"] = payload.success_url,
                ["fail_url"] = payload.fail_url,
                ["cancel_url"] = payload.cancel_url,
                ["ipn_url"] = payload.ipn_url,
                ["product_name"] = payload.product_name,
                ["product_category"] = payload.product_category,
                ["product_profile"] = payload.product_profile,
                ["cus_name"] = payload.cus_name,
                ["cus_email"] = payload.cus_email,
                ["cus_add1"] = payload.cus_add1,
                ["cus_city"] = payload.cus_city,
                ["cus_country"] = payload.cus_country,
                ["cus_phone"] = payload.cus_phone,
                ["shipping_method"] = payload.shipping_method,
                ["num_of_item"] = payload.num_of_item,
                ["value_a"] = payload.value_a,
                ["value_b"] = payload.value_b,
                ["value_c"] = payload.value_c,
                ["value_d"] = payload.value_d
            };

            var response = await client.PostAsync(
                $"{_settings.BaseUrl}/gwprocess/v4/api.php",
                new FormUrlEncodedContent(dict));

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<SSLCommerzInitResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || string.IsNullOrWhiteSpace(result.GatewayPageURL))
            {
                return Content("Payment gateway session creation failed: " + json);
            }

            payment.SessionKey = result.sessionkey;
            await _context.SaveChangesAsync();

            return Redirect(result.GatewayPageURL);
        }

        [HttpPost]
        public async Task<IActionResult> Success()
        {
            string tranId = Request.Form["tran_id"];
            string valId = Request.Form["val_id"];
            string amount = Request.Form["amount"];
            string status = Request.Form["status"];

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == tranId);

            if (payment == null)
                return Content("Payment record not found.");

            // For a real project, do server-side validation call here before marking paid.
            payment.PaymentStatus = "Paid";
            payment.PaymentDate = DateTime.Now;

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.RegistrationId == payment.RegistrationId);

            if (registration != null)
            {
                registration.RegistrationStatus = "Confirmed";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment completed successfully.";
            return RedirectToAction("MyPayment", "Participant");
        }

        [HttpPost]
        public async Task<IActionResult> Fail()
        {
            string tranId = Request.Form["tran_id"];

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == tranId);

            if (payment != null)
            {
                payment.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();
            }

            TempData["Error"] = "Payment failed.";
            return RedirectToAction("MyPayment", "Participant");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel()
        {
            string tranId = Request.Form["tran_id"];

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == tranId);

            if (payment != null)
            {
                payment.PaymentStatus = "Cancelled";
                await _context.SaveChangesAsync();
            }

            TempData["Error"] = "Payment cancelled.";
            return RedirectToAction("MyPayment", "Participant");
        }

        [HttpPost]
        public IActionResult IPN()
        {
            // You can log Request.Form here for async gateway notifications.
            return Ok();
        }
    }
}