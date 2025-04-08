using System.Net.Mail;
using System.Net;
using BirileriWebSitesi.Interfaces;

namespace BirileriWebSitesi.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var client = new SmtpClient();
            client.Host = _configuration["Smtp:Host"];
            client.Port = int.Parse(_configuration["Smtp:Port"]);
            client.Credentials = new NetworkCredential(
                _configuration["Smtp:Username"],
                _configuration["Smtp:Password"]);
            client.EnableSsl = true;

            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:From"]),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}
