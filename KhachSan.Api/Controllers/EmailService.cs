using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace KhachSan.Api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new InvalidOperationException("Thiếu cấu hình email trong appsettings.json.");
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderEmail, senderPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName ?? "Khách Sạn Đức Thịnh"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi gửi email: {ex.Message}", ex);
            }
        }
    }
}