namespace Websitebanhang.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string documentBody);
    }
}
