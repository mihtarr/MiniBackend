using SendGrid;
using SendGrid.Helpers.Mail;

namespace MiniBackend.Services
{
    public class EmailService
    {
        private readonly string _apiKey;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SENDGRID_API_KEY"];
        }

        public async Task SendResetPasswordEmail(string toEmail, string resetLink)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("stmydk@outlook.com", "Mini Game Platform");
            var subject = "Password Reset Request";
            var to = new EmailAddress(toEmail);
            var plainTextContent = $"Click the link to reset your password: {resetLink}";
            var htmlContent = $"<p>Click the link to reset your password:</p><a href='{resetLink}'>Reset Password</a>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }

        public async Task SendConfirmationEmail(string toEmail, string confirmLink)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("stmydk@outlook.com", "Mini Game Platform");
            var subject = "Confirm Your Email";
            var to = new EmailAddress(toEmail);
            var plainTextContent = $"Please confirm your email by clicking: {confirmLink}";
            var htmlContent = $"<p>Please confirm your email:</p><a href='{confirmLink}'>Confirm Email</a>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }
    }
}
