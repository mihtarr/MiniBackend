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
        var response = await client.SendEmailAsync(msg);
    }
}
}






