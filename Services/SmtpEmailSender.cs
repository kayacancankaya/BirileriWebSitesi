using System.Net.Mail;
using System.Net;
using BirileriWebSitesi.Interfaces;

namespace BirileriWebSitesi.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;
        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string email, string subject,string from, string type)
        {
            try
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
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = subject,
                    IsBodyHtml = true,
                };
                message.To.Add(email);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;   
            }
            
        }
        public async Task<bool> SendContactUsEmailAsync(string username, string email,string? phone, string message, string? subject)
        {
            try
            {
                string phoneNumber = phone ?? string.Empty;
                string subjectString = subject ?? string.Empty;

                using var client = new SmtpClient();
                client.Host = _configuration["SMTP:Host"];
                client.Port = int.Parse(_configuration["SMTP:Port"]);
                client.Credentials = new NetworkCredential(
                    _configuration["SMTP:Username"],
                    _configuration["SMTP:Password"]);
                client.EnableSsl = true;

                string htmlMessage = "Birileri Girişimci Takımı'ndan gelen iletişim formu mesajı:<br><br>" +
                    "<strong>Kullanıcı İsmi:</strong> " + username + "<br>" +
                    "<strong>Gönderen:</strong> " + email + "<br>" +
                    "<strong>Telefon:</strong> " + email + "<br>" +
                    "<strong>Konu:</strong> " + subjectString + "<br>" +
                    "<strong>Mesaj:</strong> " + message + "<br>";
                string? from = _configuration["SMTP:Username"];
                string? infoAddress = _configuration["SMTP:InfoAddress"];
                string? cc1 = _configuration["SMTP:CC1"];
                string? cc2 = _configuration["SMTP:CC2"];
                var mail = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };
                mail.To.Add(infoAddress);
                mail.CC.Add(cc1);
                mail.CC.Add(cc2);

                await client.SendMailAsync(mail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
            
        }
    }
}
