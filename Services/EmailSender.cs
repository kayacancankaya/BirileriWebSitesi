using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using BirileriWebSitesi.Interfaces;

namespace BirileriWebSitesi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;
        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string email, string subject,string from, string type)
        {
            try
            {
                //using var client = new SmtpClient();
                //client.Host = _configuration["Smtp:Host"];
                //client.Port = int.Parse(_configuration["Smtp:Port"]);
                //client.Credentials = new NetworkCredential(
                //    _configuration["Smtp:Username"],
                //    _configuration["Smtp:Password"]);
                //client.EnableSsl = true;

                //var message = new MailMessage
                //{
                //    From = new MailAddress(from),
                //    Subject = subject,
                //    Body = subject,
                //    IsBodyHtml = true,
                //};
                //message.To.Add(email);

                //await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;   
            }
            
        }
        public async Task<string> SendContactUsEmailAsync(string username, string email,string? phone, string message, string? subject)
        {
            try
            {

                string phoneNumber = phone ?? string.Empty;
                string subjectString = subject ?? string.Empty;


                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                mimeMessage.To.Add(MailboxAddress.Parse(_configuration["SMTP:InfoAddress"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC1"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC2"]));
                mimeMessage.Subject = subjectString ?? "";

                string htmlMessage = $"Birileri Girişimci Takımı'ndan gelen iletişim formu mesajı:<br><br>" +
                    $"<strong>Kullanıcı İsmi:</strong> {username}<br>" +
                    $"<strong>Gönderen:</strong> {email}<br>" +
                    $"<strong>Telefon:</strong> {phoneNumber}<br>" +
                    $"<strong>Konu:</strong> {subject}<br>" +
                    $"<strong>Mesaj:</strong> {message}<br>";

                mimeMessage.Body = new TextPart("html") { Text = htmlMessage };
                using var smtp = new SmtpClient
                {
                    Timeout = 10000 // Timeout in milliseconds (10 seconds)
                };

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(mimeMessage);
                await smtp.DisconnectAsync(true);

                return "Mail Gönderildi";

                
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
                
            }
            
        }
    }
}
