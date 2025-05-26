using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using BirileriWebSitesi.Interfaces;
using Azure.Core;
using Humanizer;

namespace BirileriWebSitesi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlMessage)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlMessage };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                smtp.Timeout = 10000; // Timeout in milliseconds (10 seconds)
                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;   
            }
            
        }
        public async Task<bool> SendPaymentEmailAsync(string to, int orderID, string paymentType)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                email.To.Add(MailboxAddress.Parse(to));
                string htmlMessage = string.Empty;
                if(paymentType="CreditCard")
                {
                    string subject = String.Format("{0} - Ödeme Alındı",orderID)
                    email.Subject = subject;
                    htmlMessage = String.Format("{0} Sipariş Numarasına ait ödemeniz başarıyla alınmıştır. 
                                                    Siparişiniz en geç 2 iş günü içerisinde hazırlanarak
                                                    kargo firmasına (Aras Kargo) teslim edilecektir. Siparişinizin durumunu
                                                    <a href="https://birilerigt.com/Identity/Account/Manage"> buradan </a>
                                                    takip edebilirsiniz.",orderID)
                }
                else
                {
                    string subject = String.Format("{0} - Hesaba Havale",orderID)
                    email.Subject = subject;   
                    htmlMessage = String.Format("{0} Siparişiniz alınmıştır. 
                                                    Banka havale formunu
                                                    <a href="https://birilerigt.com/Identity/Account/Manage"> buradan </a>
                                                    gönderdiğinizde siparişiniz işleme alınacak, en geç 2 iş günü içerisinde
                                                    kargo firmasına (Aras Kargo) teslim edilecektir.",orderID)
                }

                var builder = new BodyBuilder { HtmlBody = htmlMessage };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                smtp.Timeout = 10000; // Timeout in milliseconds (10 seconds)
                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
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

                await smtp.ConnectAsync(_configuration["SMTP:Host"], Convert.ToInt32(_configuration["SMTP:Port"]), SecureSocketOptions.None);
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
