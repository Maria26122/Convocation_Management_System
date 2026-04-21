using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QRCoder;
using System.IO;

public class PaymentController : Controller
{
    private readonly ConvocationDbContext _context;

    public PaymentController(ConvocationDbContext context)
    {
        _context = context;
    }

    public IActionResult Pay(int registrationId)
    {
        var registration = _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefault(r => r.RegistrationId == registrationId);

        if (registration == null)
            return RedirectToAction("Dashboard", "Participant");

        // SSLCommerz Sandbox URL
        string url = "https://sandbox.sslcommerz.com/gwprocess/v4/api.php";

        var store_id = "testbox";
        var store_passwd = "qwerty";

        var data = new Dictionary<string, string>
        {
            { "store_id", store_id },
            { "store_passwd", store_passwd },
            { "total_amount", registration.TotalAmount.ToString() },
            { "currency", "BDT" },
            { "tran_id", registration.RegistrationId.ToString() },

            { "success_url", "https://localhost:5001/Payment/Success" },
            { "fail_url", "https://localhost:5001/Payment/Fail" },
            { "cancel_url", "https://localhost:5001/Payment/Cancel" },

            { "cus_name", "Student" },
            { "cus_email", "student@email.com" },
            { "cus_add1", "Dhaka" },
            { "cus_phone", "01700000000" },

            { "product_name", "Convocation Registration" },
            { "product_category", "Education" },
            { "product_profile", "general" }
        };

        using (var client = new HttpClient())
        {
            var response = client.PostAsync(url, new FormUrlEncodedContent(data)).Result;
            var json = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JsonConvert.DeserializeObject<dynamic>(json);

            return Redirect(obj.GatewayPageURL.ToString());
        }
    }

    [HttpPost]
    public IActionResult Success()
    {
        var tranId = Request.Form["tran_id"];

        var registration = _context.Registrations
            .Include(r => r.Participant)
            .FirstOrDefault(r => r.RegistrationId.ToString() == tranId);

        if (registration != null)
        {
            // ✅ Update payment status
            registration.RegistrationStatus = "Paid";

            // ✅ Generate QR text
            string qrText = $"REG-{registration.RegistrationId}-USER-{registration.ParticipantId}";

            // ✅ Generate QR image
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                // Save image
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/qrcodes");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = $"qr_{registration.RegistrationId}.png";
                string filePath = Path.Combine(folderPath, fileName);

                System.IO.File.WriteAllBytes(filePath, qrBytes);

                // Save to DB
                var qrPass = new QrPass
                {
                    RegistrationId = registration.RegistrationId,
                    QrCodeText = qrText,
                    QrImagePath = "/qrcodes/" + fileName,
                    IssuedAt = DateTime.Now,
                    IsUsed = false
                };

                _context.QrPasses.Add(qrPass);
            }

            _context.SaveChanges();
        }

        return RedirectToAction("MyQRPass", "Participant");
    }
}