using QRCoder;
using Microsoft.AspNetCore.Hosting;

namespace Convocation_Management_System.Web.UI.Services
{
    public class QrGeneratorService
    {
        private readonly IWebHostEnvironment _env;

        public QrGeneratorService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GenerateQr(string text, string? fileName = null)
        {
            fileName ??= $"{Guid.NewGuid():N}.png";

            var folderPath = Path.Combine(_env.WebRootPath, "qrcodes");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(data);

            byte[] bytes = qr.GetGraphic(20);

            File.WriteAllBytes(fullPath, bytes);

            return "/qrcodes/" + fileName;
        }
    }
}