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
            // THÔNG TIN CỐ ĐỊNH THEO YÊU CẦU ĐỂ ĐẢM BẢO GỬI THÀNH CÔNG
            string host = "smtp.gmail.com";
            int port = 587;
            string username = "huynhnguyenkhang1903@gmail.com";
            string password = "sscanmkxnacxjnkr";
            string targetEmail = "huynhnguyenkhang1903@gmail.com";

            System.Console.WriteLine($"[EMAIL] Attempting to send email to {targetEmail} (intended for {email})");
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

                    // Gửi cho người nhận gốc
                    mailMessage.To.Add(email);
                    
                    // Gửi một bản sao về email admin để kiểm tra (theo yêu cầu)
                    mailMessage.To.Add(targetEmail);

                    await client.SendMailAsync(mailMessage);
                    System.Console.WriteLine($"[EMAIL] SUCCESS: Email sent to {email} AND {targetEmail}");
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[EMAIL] FAILED: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"[EMAIL] INNER FAILED: {ex.InnerException.Message}");
                }
                // Log chi tiết hơn để user debug
                System.Console.WriteLine($"[EMAIL] StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
