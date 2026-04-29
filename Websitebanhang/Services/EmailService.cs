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
            try
            {
                var smtpsettings = _configuration.GetSection("SmtpSettings");
                var host = smtpsettings["Host"];
                var port = int.Parse(smtpsettings["Port"] ?? "587");
                var username = smtpsettings["Username"];
                var password = smtpsettings["Password"];

                // Nếu chưa cấu hình email hoặc dùng giá trị mặc định, ghi ra file để test
                if (string.IsNullOrWhiteSpace(username) || username == "your_email@gmail.com")
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "TestEmail.html");
                    await File.WriteAllTextAsync(filePath, documentBody);
                    System.Console.WriteLine("--- EMAIL MOCK MODE: Check wwwroot/TestEmail.html ---");
                    return;
                }

                using (var client = new SmtpClient(host, port))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(username, "Aura Coffee"),
                        Subject = subject,
                        Body = documentBody,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"ERROR SENDING EMAIL: {ex.Message}");
            }
        }
    }
}
