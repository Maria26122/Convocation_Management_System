using QRCoder;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.Drawing.Imaging;

namespace Convocation_Management_System.Web.UI.Services
{
    public class QrGeneratorService
    {
        private readonly IWebHostEnvironment _env;

        public QrGeneratorService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GenerateQr(string text, string fileName = null)
        {
            fileName ??= Guid.NewGuid() + ".png";

            var folderPath = Path.Combine(_env.WebRootPath, "qrcodes");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);

            using Bitmap bitmap = qrCode.GetGraphic(20);

            bitmap.Save(fullPath, ImageFormat.Png);

            return "/qrcodes/" + fileName;
        }
    }
}