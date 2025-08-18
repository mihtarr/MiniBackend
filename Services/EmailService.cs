using MailKit.Net.Smtp;
using MimeKit;

namespace MiniBackend.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp-mail.outlook.com";
        private readonly int _port = 587;
        private readonly string _from = "stmydk@outlook.com";
        private readonly string _password = "@gu@44JF3j&2Ey;"; // Gmail App Password

        public void SendResetPasswordEmail(string to, string resetLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Mini Game Platform", _from));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = "Password Reset Request";
            message.Body = new TextPart("plain")
            {
                Text = $"You requested a password reset.\nClick the link below to reset your password:\n{resetLink}\n\n" +
                       "If you did not request this, please secure your account."
            };

            using var client = new SmtpClient();
            client.Connect(_smtpServer, _port, false);
            client.Authenticate(_from, _password);
            client.Send(message);
            client.Disconnect(true);
        }
    }
}
