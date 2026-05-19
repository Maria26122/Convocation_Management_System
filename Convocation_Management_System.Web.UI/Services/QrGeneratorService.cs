using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Convocation_Management_System.Web.UI.Services
{
    public class QrGeneratorService
    {
        public string GenerateQr(string text, string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Guid.NewGuid().ToString() + ".png";
            }
            var path = Path.Combine("wwwroot/qrcodes");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var fullPath = Path.Combine(path, fileName);

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using Bitmap bitmap = qrCode.GetGraphic(20);

            bitmap.Save(fullPath, ImageFormat.Png);

            return "/qrcodes/" + fileName;
        }
    }
}