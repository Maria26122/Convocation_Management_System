using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Convocation_Management_System.Web.UI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath = null)
        {
            var host = _config["EmailSettings:Host"];
            var port = int.Parse(_config["EmailSettings:Port"]);
            var email = _config["EmailSettings:Email"];
            var password = _config["EmailSettings:Password"];
            var displayName = _config["EmailSettings:DisplayName"];

            using var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(email, displayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            // Attach QR image if exists
            if (!string.IsNullOrEmpty(attachmentPath))
            {
                var fullPath = Path.Combine("wwwroot", attachmentPath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    message.Attachments.Add(new Attachment(fullPath));
                }
            }

            await smtp.SendMailAsync(message);
        }
    }
}
