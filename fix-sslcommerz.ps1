# Run this from the solution root folder: Convocation_Management_System
$ErrorActionPreference = "Stop"

$path = "Convocation_Management_System.Web.UI/Controllers/PaymentController.cs"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
using System.Globalization;
using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ConvocationDbContext _context;
        private readonly StudentPaymentService _paymentService;

        public PaymentController(ConvocationDbContext context, StudentPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }


        // Admin payment list and manual payment management.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .OrderByDescending(p => p.PaymentId)
                .ToListAsync();

            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var payment = await LoadPaymentAsync(id.Value);
            return payment == null ? NotFound() : View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateRegistrationDropDownAsync();
            return View(new Payment
            {
                PaymentMethod = "SSLCommerz",
                PaymentStatus = "Pending",
                PaymentDate = null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentId,RegistrationId,PaidAmount,PaymentMethod,TransactionId,PaymentStatus,PaymentDate,SessionKey,QrPass")] Payment payment)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRegistrationDropDownAsync(payment.RegistrationId);
                return View(payment);
            }

            _context.Payment.Add(payment);
            await SyncRegistrationStatusAsync(payment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var payment = await _context.Payment.FindAsync(id.Value);
            if (payment == null)
                return NotFound();

            await PopulateRegistrationDropDownAsync(payment.RegistrationId);
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,RegistrationId,PaidAmount,PaymentMethod,TransactionId,PaymentStatus,PaymentDate,SessionKey,QrPass")] Payment payment)
        {
            if (id != payment.PaymentId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateRegistrationDropDownAsync(payment.RegistrationId);
                return View(payment);
            }

            try
            {
                _context.Update(payment);
                await SyncRegistrationStatusAsync(payment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Payment.AnyAsync(e => e.PaymentId == payment.PaymentId))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var payment = await LoadPaymentAsync(id.Value);
            return payment == null ? NotFound() : View(payment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payment.FindAsync(id);
            if (payment != null)
            {
                _context.Payment.Remove(payment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Shows the local confirmation page before sending the student to SSLCommerz.
        [HttpGet]
        public async Task<IActionResult> PayNow(int registrationId)
        {
            var registration = await LoadRegistrationAsync(registrationId);

            if (registration == null)
            {
                TempData["Error"] = "Registration was not found.";
                return RedirectToAction("MyPayment", "Participant");
            }

            var existingPayment = await _context.Payment
                .FirstOrDefaultAsync(p => p.RegistrationId == registrationId);

            if (existingPayment?.PaymentStatus == "Paid")
            {
                TempData["Success"] = "Payment is already completed.";
                return RedirectToAction("MyPayment", "Participant");
            }

            return View(registration);
        }

        // Creates/updates a local pending payment, creates the SSLCommerz session,
        // then redirects the browser to GatewayPageURL.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePayment(int registrationId)
        {
            var registration = await LoadRegistrationAsync(registrationId);

            if (registration == null)
            {
                TempData["Error"] = "Registration was not found.";
                return RedirectToAction("MyPayment", "Participant");
            }

            if (registration.TotalAmount < 10)
            {
                TempData["Error"] = "Payment amount must be at least 10.00 BDT for SSLCommerz.";
                return RedirectToAction(nameof(PayNow), new { registrationId });
            }

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.RegistrationId == registrationId);

            if (payment?.PaymentStatus == "Paid")
            {
                TempData["Success"] = "Payment is already completed.";
                return RedirectToAction("MyPayment", "Participant");
            }

            var transactionId = BuildTransactionId(registrationId);

            if (payment == null)
            {
                payment = new Payment
                {
                    RegistrationId = registrationId,
                    PaidAmount = registration.TotalAmount,
                    PaymentMethod = "SSLCommerz",
                    PaymentStatus = "Pending",
                    TransactionId = transactionId,
                    PaymentDate = null,
                    SessionKey = null
                };

                _context.Payment.Add(payment);
            }
            else
            {
                payment.PaidAmount = registration.TotalAmount;
                payment.PaymentMethod = "SSLCommerz";
                payment.PaymentStatus = "Pending";
                payment.TransactionId = transactionId;
                payment.PaymentDate = null;
                payment.SessionKey = null;
            }

            await _context.SaveChangesAsync();

            try
            {
                var request = new SSLCommerzPaymentRequest
                {
                    TransactionId = transactionId,
                    Amount = registration.TotalAmount,
                    SuccessUrl = Url.Action(nameof(PaymentSuccess), "Payment", null, Request.Scheme)!,
                    FailUrl = Url.Action(nameof(PaymentFail), "Payment", null, Request.Scheme)!,
                    CancelUrl = Url.Action(nameof(PaymentCancel), "Payment", null, Request.Scheme)!,
                    IpnUrl = Url.Action(nameof(Ipn), "Payment", null, Request.Scheme)!,
                    CustomerName = registration.Participant?.UserAccount?.FullName,
                    CustomerEmail = registration.Participant?.UserAccount?.Email,
                    CustomerPhone = registration.Participant?.UserAccount?.Phone,
                    CustomerAddress = "Dhaka",
                    CustomerCity = "Dhaka",
                    CustomerState = "Dhaka",
                    CustomerPostCode = "1200",
                    ProductName = $"Convocation Registration #{registration.RegistrationId}",
                    ProductCategory = "Education"
                };

                var sslCommerzResponse = await _paymentService.InitiatePaymentAsync(request);
                payment.SessionKey = sslCommerzResponse.SessionKey;
                await _context.SaveChangesAsync();

                return Redirect(sslCommerzResponse.GatewayPageUrl);
            }
            catch (Exception ex)
            {
                payment.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();

                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(PayNow), new { registrationId });
            }
        }

        [AcceptVerbs("GET", "POST")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentSuccess()
        {
            var result = await FinalizeSuccessfulPaymentAsync();

            if (result)
                TempData["Success"] = "Payment completed successfully.";
            else
                TempData["Error"] = "Payment was returned as success, but SSLCommerz validation failed.";

            return RedirectToAction("MyPayment", "Participant");
        }

        [AcceptVerbs("GET", "POST")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentFail()
        {
            await MarkPaymentStatusFromCallbackAsync("Failed");
            TempData["Error"] = "Payment failed. Please try again.";
            return RedirectToAction("MyPayment", "Participant");
        }

        [AcceptVerbs("GET", "POST")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentCancel()
        {
            await MarkPaymentStatusFromCallbackAsync("Failed");
            TempData["Error"] = "Payment was cancelled.";
            return RedirectToAction("MyPayment", "Participant");
        }

        // Server-to-server payment notification endpoint. Configure this URL in SSLCommerz IPN settings.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Ipn()
        {
            var result = await FinalizeSuccessfulPaymentAsync();
            return Ok(new { processed = result });
        }

        private async Task<bool> FinalizeSuccessfulPaymentAsync()
        {
            var transactionId = ReadCallbackValue("tran_id", "tranId", "transactionId");
            var valId = ReadCallbackValue("val_id", "valId");

            if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(valId))
                return false;

            var payment = await _context.Payment
                .Include(p => p.Registration)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
                return false;

            try
            {
                var validationResponse = await _paymentService.ValidatePaymentAsync(valId);
                if (!IsValidSslCommerzPayment(validationResponse, payment))
                {
                    payment.PaymentStatus = "Failed";
                    await _context.SaveChangesAsync();
                    return false;
                }

                payment.PaymentStatus = "Paid";
                payment.PaymentDate = DateTime.Now;
                payment.PaymentMethod = string.IsNullOrWhiteSpace(validationResponse?.card_type)
                    ? "SSLCommerz"
                    : validationResponse.card_type;

                if (payment.Registration != null)
                    payment.Registration.RegistrationStatus = "Paid";

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                payment.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();
                return false;
            }
        }

        private async Task MarkPaymentStatusFromCallbackAsync(string status)
        {
            var transactionId = ReadCallbackValue("tran_id", "tranId", "transactionId");
            if (string.IsNullOrWhiteSpace(transactionId))
                return;

            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
                return;

            payment.PaymentStatus = status;
            payment.PaymentDate = null;
            await _context.SaveChangesAsync();
        }

        private bool IsValidSslCommerzPayment(SSLCommerzValidationResponse? response, Payment payment)
        {
            if (response == null)
                return false;

            var statusValid = string.Equals(response.status, "VALID", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(response.status, "VALIDATED", StringComparison.OrdinalIgnoreCase);

            if (!statusValid)
                return false;

            if (!string.Equals(response.tran_id, payment.TransactionId, StringComparison.OrdinalIgnoreCase))
                return false;

            var amountText = response.currency == "BDT" || string.IsNullOrWhiteSpace(response.currency_amount)
                ? response.amount
                : response.currency_amount;

            if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var paidAmount))
                return false;

            return Math.Abs(paidAmount - payment.PaidAmount) < 1;
        }

        private async Task<Registration?> LoadRegistrationAsync(int registrationId)
        {
            return await _context.Registration
                .Include(r => r.Event)
                .Include(r => r.Participant)
                    .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);
        }

        private string ReadCallbackValue(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (Request.HasFormContentType && Request.Form.TryGetValue(key, out var formValue))
                {
                    var value = formValue.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                if (Request.Query.TryGetValue(key, out var queryValue))
                {
                    var value = queryValue.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return string.Empty;
        }


        private async Task<Payment?> LoadPaymentAsync(int paymentId)
        {
            return await _context.Payment
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Participant)
                .Include(p => p.Registration)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        private async Task PopulateRegistrationDropDownAsync(int? selectedRegistrationId = null)
        {
            var registrations = await _context.Registration
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegistrationDate)
                .Select(r => new
                {
                    r.RegistrationId,
                    DisplayText = "#" + r.RegistrationId + " - " +
                                  (r.Participant != null ? r.Participant.StudentId : "Student") + " - " +
                                  (r.Event != null ? r.Event.EventTitle : "Event")
                })
                .ToListAsync();

            ViewBag.RegistrationId = new SelectList(registrations, "RegistrationId", "DisplayText", selectedRegistrationId);
        }

        private async Task SyncRegistrationStatusAsync(Payment payment)
        {
            var registration = await _context.Registration.FindAsync(payment.RegistrationId);
            if (registration == null)
                return;

            if (payment.PaymentStatus == "Paid")
                registration.RegistrationStatus = "Paid";
            else if (registration.RegistrationStatus == "Paid" && payment.PaymentStatus != "Paid")
                registration.RegistrationStatus = "Pending";
        }

        private static string BuildTransactionId(int registrationId)
        {
            // SSLCommerz tran_id has a short max length; keep this compact and unique.
            return $"CNV{registrationId}{DateTime.UtcNow:yyMMddHHmmss}";
        }
    }
}
'@

$path = "Convocation_Management_System.Web.UI/Helpers/StudentPaymentService.cs"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Convocation_Management_System.Web.UI.Helpers
{
    public class StudentPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public StudentPaymentService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<SSLCommerzInitResult> InitiatePaymentAsync(SSLCommerzPaymentRequest request)
        {
            if (request.Amount < 10)
                throw new InvalidOperationException("SSLCommerz requires the transaction amount to be at least 10.00 BDT.");

            var storeId = GetRequiredSetting("SSLCommerz:StoreId");
            var storePassword = GetRequiredSetting("SSLCommerz:StorePassword");
            var baseUrl = GetBaseUrl();

            var postData = new Dictionary<string, string>
            {
                ["store_id"] = storeId,
                ["store_passwd"] = storePassword,
                ["total_amount"] = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["currency"] = "BDT",
                ["tran_id"] = request.TransactionId,
                ["success_url"] = request.SuccessUrl,
                ["fail_url"] = request.FailUrl,
                ["cancel_url"] = request.CancelUrl,
                ["ipn_url"] = request.IpnUrl,

                ["cus_name"] = SafeValue(request.CustomerName, "Student"),
                ["cus_email"] = SafeValue(request.CustomerEmail, "student@example.com"),
                ["cus_add1"] = SafeValue(request.CustomerAddress, "Dhaka"),
                ["cus_city"] = SafeValue(request.CustomerCity, "Dhaka"),
                ["cus_state"] = SafeValue(request.CustomerState, "Dhaka"),
                ["cus_postcode"] = SafeValue(request.CustomerPostCode, "1200"),
                ["cus_country"] = "Bangladesh",
                ["cus_phone"] = SafeValue(request.CustomerPhone, "01700000000"),

                ["shipping_method"] = "NO",
                ["num_of_item"] = "1",
                ["product_name"] = SafeValue(request.ProductName, "Convocation Registration"),
                ["product_category"] = SafeValue(request.ProductCategory, "Education"),
                ["product_profile"] = "non-physical-goods"
            };

            using var content = new FormUrlEncodedContent(postData);
            using var response = await _httpClient.PostAsync($"{baseUrl}/gwprocess/v4/api.php", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SSLCommerz returned HTTP {(int)response.StatusCode}. Response: {responseBody}");

            SSLCommerzInitResponse? initResponse;
            try
            {
                initResponse = JsonSerializer.Deserialize<SSLCommerzInitResponse>(responseBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"SSLCommerz returned an invalid JSON response: {responseBody}", ex);
            }

            if (initResponse == null || !string.Equals(initResponse.status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                var reason = initResponse?.failedreason;
                throw new InvalidOperationException($"Payment initiation failed. SSLCommerz response: {reason ?? responseBody}");
            }

            if (string.IsNullOrWhiteSpace(initResponse.GatewayPageURL))
                throw new InvalidOperationException($"Payment initiation succeeded but GatewayPageURL was missing. Response: {responseBody}");

            return new SSLCommerzInitResult
            {
                GatewayPageUrl = initResponse.GatewayPageURL,
                SessionKey = initResponse.sessionkey,
                RawResponse = responseBody
            };
        }

        public async Task<SSLCommerzValidationResponse?> ValidatePaymentAsync(string valId)
        {
            if (string.IsNullOrWhiteSpace(valId))
                return null;

            var storeId = Uri.EscapeDataString(GetRequiredSetting("SSLCommerz:StoreId"));
            var storePassword = Uri.EscapeDataString(GetRequiredSetting("SSLCommerz:StorePassword"));
            var encodedValId = Uri.EscapeDataString(valId);
            var baseUrl = GetBaseUrl();

            var validationUrl = $"{baseUrl}/validator/api/validationserverAPI.php?val_id={encodedValId}&store_id={storeId}&store_passwd={storePassword}&v=1&format=json";

            using var response = await _httpClient.GetAsync(validationUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SSLCommerz validation returned HTTP {(int)response.StatusCode}. Response: {responseBody}");

            return JsonSerializer.Deserialize<SSLCommerzValidationResponse>(responseBody, _jsonOptions);
        }

        private string GetBaseUrl()
        {
            var configuredBaseUrl = _configuration["SSLCommerz:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
                return configuredBaseUrl.TrimEnd('/');

            var isLiveText = _configuration["SSLCommerz:IsLive"];
            var isLive = bool.TryParse(isLiveText, out var parsed) && parsed;
            return isLive ? "https://securepay.sslcommerz.com" : "https://sandbox.sslcommerz.com";
        }

        private string GetRequiredSetting(string key)
        {
            var value = _configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Missing required configuration value: {key}");

            return value;
        }

        private static string SafeValue(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public class SSLCommerzPaymentRequest
    {
        public required string TransactionId { get; set; }
        public required decimal Amount { get; set; }
        public required string SuccessUrl { get; set; }
        public required string FailUrl { get; set; }
        public required string CancelUrl { get; set; }
        public required string IpnUrl { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerState { get; set; }
        public string? CustomerPostCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCategory { get; set; }
    }

    public class SSLCommerzInitResult
    {
        public required string GatewayPageUrl { get; set; }
        public string? SessionKey { get; set; }
        public string? RawResponse { get; set; }
    }

    public class SSLCommerzInitResponse
    {
        public string? status { get; set; }
        public string? failedreason { get; set; }
        public string? sessionkey { get; set; }
        public string? GatewayPageURL { get; set; }
    }

    public class SSLCommerzValidationResponse
    {
        public string? status { get; set; }
        public string? tran_id { get; set; }
        public string? val_id { get; set; }
        public string? amount { get; set; }
        public string? currency { get; set; }
        public string? currency_amount { get; set; }
        public string? card_type { get; set; }
        public string? bank_tran_id { get; set; }
        public string? risk_level { get; set; }
        public string? risk_title { get; set; }
    }
}
'@

$path = "Convocation_Management_System.Web.UI/Views/Payment/PayNow.cshtml"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
@model Convocation.Entities.Registration

@{
    Layout = "~/Views/Shared/_ParticipantLayout.cshtml";
    ViewData["Title"] = "Confirm Payment";
}

<div class="container mt-4">
    <div class="card shadow-sm border-0">
        <div class="card-header bg-primary text-white">
            <h4 class="mb-0">Confirm Payment</h4>
        </div>
        <div class="card-body">
            @if (TempData["Error"] != null)
            {
                <div class="alert alert-danger">@TempData["Error"]</div>
            }

            <p>Please review your convocation payment details before continuing to SSLCommerz.</p>

            <table class="table table-bordered">
                <tr>
                    <th style="width: 220px;">Registration ID</th>
                    <td>@Model.RegistrationId</td>
                </tr>
                <tr>
                    <th>Event</th>
                    <td>@Model.Event?.EventTitle</td>
                </tr>
                <tr>
                    <th>Guest Count</th>
                    <td>@Model.GuestCount</td>
                </tr>
                <tr>
                    <th>Total Amount</th>
                    <td><strong>@Model.TotalAmount.ToString("0.00") BDT</strong></td>
                </tr>
                <tr>
                    <th>Status</th>
                    <td>@Model.RegistrationStatus</td>
                </tr>
            </table>

            <form asp-controller="Payment" asp-action="InitiatePayment" method="post">
                @Html.AntiForgeryToken()
                <input type="hidden" name="registrationId" value="@Model.RegistrationId" />
                <button type="submit" class="btn btn-success">
                    Proceed to SSLCommerz Payment
                </button>
                <a asp-controller="Participant" asp-action="MyPayment" class="btn btn-secondary ms-2">
                    Back
                </a>
            </form>
        </div>
    </div>
</div>
'@

$path = "Convocation_Management_System.Web.UI/Views/Payment/Create.cshtml"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
@model Convocation.Entities.Payment

@{
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
    ViewData["Title"] = "Create Payment";
}

<div class="form-card">
    <h3 class="form-title mb-4">Create Payment</h3>

    <form asp-action="Create" method="post">
        <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

        <div class="row g-3">
            <div class="col-md-6">
                <label asp-for="RegistrationId" class="form-label"></label>
                <select asp-for="RegistrationId" class="form-select" asp-items="ViewBag.RegistrationId">
                    <option value="">-- Select Registration --</option>
                </select>
                <span asp-validation-for="RegistrationId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaidAmount" class="form-label"></label>
                <input asp-for="PaidAmount" class="form-control" />
                <span asp-validation-for="PaidAmount" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaymentMethod" class="form-label"></label>
                <input asp-for="PaymentMethod" class="form-control" />
                <span asp-validation-for="PaymentMethod" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="TransactionId" class="form-label"></label>
                <input asp-for="TransactionId" class="form-control" />
                <span asp-validation-for="TransactionId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaymentStatus" class="form-label"></label>
                <select asp-for="PaymentStatus" class="form-select">
                    <option value="Pending">Pending</option>
                    <option value="Paid">Paid</option>
                    <option value="Failed">Failed</option>
                </select>
                <span asp-validation-for="PaymentStatus" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaymentDate" class="form-label"></label>
                <input asp-for="PaymentDate" type="datetime-local" class="form-control" />
                <span asp-validation-for="PaymentDate" class="text-danger"></span>
            </div>
        </div>

        <div class="mt-4">
            <button type="submit" class="btn btn-primary">Create</button>
            <a asp-action="Index" class="btn btn-secondary">Back to List</a>
        </div>
    </form>
</div>

@section Scripts {
    @{ await Html.RenderPartialAsync("_ValidationScriptsPartial"); }
}
'@

$path = "Convocation_Management_System.Web.UI/Views/Payment/Edit.cshtml"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
@model Convocation.Entities.Payment

@{
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
    ViewData["Title"] = "Edit Payment";
}

<div class="form-card">
    <h3 class="form-title mb-4">Edit Payment</h3>

    <form asp-action="Edit" method="post">
        <input type="hidden" asp-for="PaymentId" />
        <input type="hidden" asp-for="SessionKey" />
        <input type="hidden" asp-for="QrPass" />
        <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

        <div class="row g-3">
            <div class="col-md-6">
                <label asp-for="RegistrationId" class="form-label"></label>
                <select asp-for="RegistrationId" class="form-select" asp-items="ViewBag.RegistrationId">
                    <option value="">-- Select Registration --</option>
                </select>
                <span asp-validation-for="RegistrationId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaidAmount" class="form-label"></label>
                <input asp-for="PaidAmount" class="form-control" />
                <span asp-validation-for="PaidAmount" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaymentMethod" class="form-label"></label>
                <input asp-for="PaymentMethod" class="form-control" />
                <span asp-validation-for="PaymentMethod" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="TransactionId" class="form-label"></label>
                <input asp-for="TransactionId" class="form-control" />
                <span asp-validation-for="TransactionId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaymentStatus" class="form-label"></label>
                <select asp-for="PaymentStatus" class="form-select">
                    <option value="Pending">Pending</option>
                    <option value="Paid">Paid</option>
                    <option value="Failed">Failed</option>
                </select>
                <span asp-validation-for="PaymentStatus" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="PaymentDate" class="form-label"></label>
                <input asp-for="PaymentDate" type="datetime-local" class="form-control" />
                <span asp-validation-for="PaymentDate" class="text-danger"></span>
            </div>
        </div>

        <div class="mt-4">
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-action="Index" class="btn btn-secondary">Back to List</a>
        </div>
    </form>
</div>

@section Scripts {
    @{ await Html.RenderPartialAsync("_ValidationScriptsPartial"); }
}
'@

$path = "Convocation_Management_System.Web.UI/Views/Registration/Details.cshtml"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
@model Convocation.Entities.Registration

@{
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
    ViewData["Title"] = "Edit Registration";
}

<div class="form-card">
    <h3 class="form-title mb-4">Edit Registration</h3>

    <form asp-action="Edit" method="post">
        <input type="hidden" asp-for="RegistrationId" />
        <input type="hidden" asp-for="RegistrationDate" />

        <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

        <div class="row g-3">
            <div class="col-md-6">
                <label asp-for="ParticipantId" class="form-label"></label>
                <select asp-for="ParticipantId" class="form-select" asp-items="ViewBag.ParticipantId">
                    <option value="">-- Select Participant --</option>
                </select>
                <span asp-validation-for="ParticipantId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="EventId" class="form-label"></label>
                <select asp-for="EventId" class="form-select" asp-items="ViewBag.EventId">
                    <option value="">-- Select Event --</option>
                </select>
                <span asp-validation-for="EventId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="GuestCount" class="form-label"></label>
                <input asp-for="GuestCount" class="form-control" />
                <span asp-validation-for="GuestCount" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="TotalAmount" class="form-label"></label>
                <input asp-for="TotalAmount" class="form-control" />
                <span asp-validation-for="TotalAmount" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="RegistrationStatus" class="form-label"></label>
                <select asp-for="RegistrationStatus" class="form-select">
                    <option value="">-- Select Status --</option>
                    <option value="Pending">Pending</option>
                    <option value="Paid">Paid</option>
                    <option value="Confirmed">Confirmed</option>
                    <option value="Rejected">Rejected</option>
                </select>
                <span asp-validation-for="RegistrationStatus" class="text-danger"></span>
            </div>
        </div>

        <div class="mt-4">
            <button type="submit" class="btn btn-primary">Update</button>
            <a asp-action="Index" class="btn btn-secondary">Back to List</a>
        </div>
    </form>
</div>

@section Scripts {
    @{
        await Html.RenderPartialAsync("_ValidationScriptsPartial");
    }
}
'@

$path = "Convocation_Management_System.Web.UI/Views/Registration/Edit.cshtml"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
@model Convocation.Entities.Registration

@{
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
    ViewData["Title"] = "Edit Registration";
}

<div class="form-card">
    <h3 class="form-title mb-4">Edit Registration</h3>

    <form asp-action="Edit"
          method="post">
        <input type="hidden" asp-for="RegistrationId" />
        <input type="hidden"
               asp-for="RegistrationDate" />
        <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

        <div class="row g-3">
            <div class="col-md-6">
                <label asp-for="EventId" class="form-label"></label>
                <select asp-for="EventId" class="form-select" id="eventSelect">
                    <option value="">-- Select Event --</option>
                    @foreach (var item in ViewBag.Events)
                    {
                        <option value="@item.EventId"
                                data-basefee="@item.BaseFee"
                                data-guestfee="@item.GuestFee"
                                selected="@(item.EventId == Model.EventId ? "selected" : null)">
                            @item.EventTitle - @Convert.ToDateTime(item.EventDate).ToString("dd MMM yyyy")
                        </option>
                    }
                </select>
                <span asp-validation-for="EventId" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="GuestCount" class="form-label"></label>
                <input asp-for="GuestCount" class="form-control" />
                <span asp-validation-for="GuestCount" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="TotalAmount" class="form-label"></label>
                <input asp-for="TotalAmount" class="form-control" readonly />
                <span asp-validation-for="TotalAmount" class="text-danger"></span>
            </div>

            <div class="col-md-6">
                <label asp-for="RegistrationStatus" class="form-label"></label>
                <select asp-for="RegistrationStatus" class="form-select">
                    <option value="">-- Select Status --</option>
                    <option value="Pending">Pending</option>
                    <option value="Paid">Paid</option>
                    <option value="Confirmed">Confirmed</option>
                    <option value="Rejected">Rejected</option>
                </select>
                <span asp-validation-for="RegistrationStatus" class="text-danger"></span>
            </div>
        </div>

        <div class="mt-4">
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-action="Index" class="btn btn-secondary">Back to List</a>
        </div>
    </form>
</div>

@section Scripts {
    @{
        await Html.RenderPartialAsync("_ValidationScriptsPartial");
    }

    <script>
        function calculateAmount() {
            var eventSelect = document.getElementById("eventSelect");
            var guestCountInput = document.getElementById("GuestCount");
            var totalAmountInput = document.getElementById("TotalAmount");

            if (!eventSelect || !guestCountInput || !totalAmountInput) return;

            var selectedOption = eventSelect.options[eventSelect.selectedIndex];

            var baseFee = parseFloat(selectedOption.getAttribute("data-basefee")) || 0;
            var guestFee = parseFloat(selectedOption.getAttribute("data-guestfee")) || 0;
            var guestCount = parseInt(guestCountInput.value) || 0;

            if (guestCount < 0) guestCount = 0;

            var total = baseFee + (guestCount * guestFee);
            totalAmountInput.value = total.toFixed(2);
        }

        document.addEventListener("DOMContentLoaded", function () {
            var eventSelect = document.getElementById("eventSelect");
            var guestCountInput = document.getElementById("GuestCount");

            if (eventSelect) {
                eventSelect.addEventListener("change", calculateAmount);
            }

            if (guestCountInput) {
                guestCountInput.addEventListener("input", calculateAmount);
            }

            calculateAmount();
        });
    </script>
}
'@

$path = "PAYMENT_MODULE_NOTES.md"
$folder = Split-Path $path -Parent
if ($folder -and !(Test-Path $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Set-Content -Path $path -Encoding UTF8 -Value @'
# SSLCommerz Payment Module Notes

## Main problem fixed

The old student payment flow did not open the SSLCommerz interface because `Payment/PayNow` returned JSON instead of redirecting the browser to SSLCommerz. The SSLCommerz API call also used an incomplete/manual URL approach instead of creating a proper hosted checkout session.

## Correct payment flow now

1. Student clicks payment button from `Participant/MyPayment`.
2. Browser opens `Payment/PayNow?registrationId=...`.
3. Student confirms payment.
4. `Payment/InitiatePayment` creates or updates a local `Payment` row as `Pending`.
5. Server posts required data to:

   `https://sandbox.sslcommerz.com/gwprocess/v4/api.php`

6. SSLCommerz returns `GatewayPageURL`.
7. Browser redirects to `GatewayPageURL`.
8. SSLCommerz posts back to success, fail, cancel, or IPN URL.
9. Success/IPN validates payment using SSLCommerz validation API.
10. If valid, payment status becomes `Paid` and registration status becomes `Paid`.

## Files changed

- `Convocation_Management_System.Web.UI/Controllers/PaymentController.cs`
- `Convocation_Management_System.Web.UI/Helpers/StudentPaymentService.cs`
- `Convocation_Management_System.Web.UI/Views/Payment/PayNow.cshtml`
- `Convocation_Management_System.Web.UI/Views/Payment/Create.cshtml`
- `Convocation_Management_System.Web.UI/Views/Payment/Edit.cshtml`
- `Convocation_Management_System.Web.UI/Views/Registration/Details.cshtml`
- `Convocation_Management_System.Web.UI/Views/Registration/Edit.cshtml`

## Files safe to remove

These files duplicated or conflicted with the actual payment module and are not needed:

- `Convocation_Management_System.Web.UI/Controllers/StudentPaymentController.cs`
- `Convocation_Management_System.Web.UI/Helpers/SSLCommercePayment.cs`
- `Convocation.Entities/Models/StudentPayment.cs`

Generated/local-only folders that should not be submitted:

- `.git`
- `.vs`
- `bin`
- `obj`

## Local testing note

Payment redirect can work from localhost, but SSLCommerz IPN cannot reach localhost from the internet. For full IPN testing, use a public tunnel such as ngrok and update the application URL accordingly.
'@

# Delete duplicate/unnecessary payment files
if (Test-Path "Convocation_Management_System.Web.UI/Controllers/StudentPaymentController.cs") { Remove-Item "Convocation_Management_System.Web.UI/Controllers/StudentPaymentController.cs" -Force }
if (Test-Path "Convocation_Management_System.Web.UI/Helpers/SSLCommercePayment.cs") { Remove-Item "Convocation_Management_System.Web.UI/Helpers/SSLCommercePayment.cs" -Force }
if (Test-Path "Convocation.Entities/Models/StudentPayment.cs") { Remove-Item "Convocation.Entities/Models/StudentPayment.cs" -Force }

Write-Host "SSLCommerz payment module files updated successfully."
Write-Host "Now run: dotnet restore; dotnet build; dotnet run --project Convocation_Management_System.Web.UI"
