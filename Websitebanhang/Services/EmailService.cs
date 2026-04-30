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
            try
            {
                // Lấy cấu hình từ Database (Ưu tiên)
                var host = await _settingService.GetSettingAsync("SmtpHost", _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com");
                var portStr = await _settingService.GetSettingAsync("SmtpPort", _configuration["SmtpSettings:Port"] ?? "587");
                var username = await _settingService.GetSettingAsync("SmtpUser", _configuration["SmtpSettings:Username"] ?? "");
                var password = await _settingService.GetSettingAsync("SmtpPass", _configuration["SmtpSettings:Password"] ?? "");

                int port = int.TryParse(portStr, out int p) ? p : 587;


                using (var client = new SmtpClient(host, port))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(username, "Aura Coffee"),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(email);

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
