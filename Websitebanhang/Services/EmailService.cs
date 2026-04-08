using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Websitebanhang.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string documentBody)
        {
            var smtpsettings = _configuration.GetSection("SmtpSettings");
            var host = smtpsettings["Host"];
            var port = int.Parse(smtpsettings["Port"] ?? "587");
            var username = smtpsettings["Username"];
            var password = smtpsettings["Password"];

            // MOCK MODE: Chỉ dành cho việc test nhanh giao diện email mà không cần cài đặt Smtp thật
            if (username == "your_email@gmail.com")
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "TestEmail.html");
                await File.WriteAllTextAsync(filePath, documentBody);
                return;
            }

            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(username ?? "noreply@coffeeshop.com", "Coffee Shop Admin"),
                    Subject = subject,
                    Body = documentBody,
                    IsBodyHtml = true,
                };
                
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
