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
        private readonly IWebsiteSettingService _settingService;

        public EmailService(IConfiguration configuration, IWebsiteSettingService settingService)
        {
            _configuration = configuration;
            _settingService = settingService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            string host = smtpSettings["Host"] ?? "smtp.gmail.com";
            int port = int.Parse(smtpSettings["Port"] ?? "587");
            string username = smtpSettings["Username"] ?? "";
            string password = smtpSettings["Password"] ?? "";

            // Xóa dấu cách nếu người dùng vô tình copy vào appsettings.json
            password = password.Replace(" ", "");

            System.Console.WriteLine($"[EMAIL] Attempting to send email to {email}");
            System.Console.WriteLine($"[EMAIL] Using SMTP Host: {host}, Port: {port}, User: {username}");

            try
            {
                using (var client = new SmtpClient(host, port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(username, "Aura Coffee"),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(email);
                    
                    await client.SendMailAsync(mailMessage);
                    System.Console.WriteLine($"[EMAIL] SUCCESS: Email sent to {email}");
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[EMAIL] FAILED: {ex.Message}");
                throw;
            }
        }
    }
}
