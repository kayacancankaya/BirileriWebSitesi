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
                    string subject = $"{orderID} - Ödeme Alındı";
                    email.Subject = subject;
                     htmlMessage = $@"
                    <table style='max-width:600px;margin:auto;font-family:Arial,sans-serif;border:1px solid #ddd;border-radius:8px;padding:20px;'>
                        <tr>
                            <td style='text-align:center;'>
                                <h2 style='color:#007bff;'>Ödemeniz Alındı</h2>
                                <p style='font-size:16px;'>Sipariş Numaranız: <strong>{orderID}</strong></p>
                                <p style='font-size:16px;'>Ödemeniz başarıyla alınmıştır. Siparişiniz en geç 2 iş günü içinde hazırlanarak Aras Kargo’ya teslim edilecektir.</p>
                                <p style='font-size:16px;'>Sipariş durumunuzu <a href='https://birilerigt.com/Identity/Account/Manage' style='color:#007bff;'>buradan</a> takip edebilirsiniz.</p>
                            </td>
                        </tr>
                    </table>";
                }
                
                else
                {
                   string subject = $"{orderID} - Hesaba Havale";
                    email.Subject = subject;
                    htmlMessage = $@"
                    <table style='max-width:600px;margin:auto;font-family:Arial,sans-serif;border:1px solid #ddd;border-radius:8px;padding:20px;'>
                        <tr>
                            <td style='text-align:center;'>
                                <h2 style='color:#ff9800;'>Havale Bekleniyor</h2>
                                <p style='font-size:16px;'>Sipariş Numaranız: <strong>{orderID}</strong></p>
                                <p style='font-size:16px;'>Siparişiniz alınmıştır. Lütfen banka havale formunu 
                                <a href='https://birilerigt.com/Identity/Account/Manage' style='color:#007bff;'>buradan</a> gönderiniz.</p>
                                <p style='font-size:16px;'>Form gönderildikten sonra siparişiniz işleme alınacak ve en geç 2 iş günü içinde Aras Kargo’ya teslim edilecektir.</p>
                            </td>
                        </tr>
                    </table>";
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

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect), SecureSocketOptions.None);
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
        public async Task<string> SendBankTransferNoticeEmailAsync(Order order, string note)
        {
            try
            {

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                mimeMessage.To.Add(MailboxAddress.Parse(_configuration["SMTP:InfoAddress"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC1"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC2"]));

                 mimeMessage.Subject = $"{order.Id} Siparişi Havale Formu";

                    // Build HTML content
                    var sb = new StringBuilder();
                    sb.AppendLine("<h3>Sipariş Bilgileri:</h3><ul>");
            
                    foreach (var item in order.Items)
                    {
                        sb.AppendLine($"<li>{item.ProductName} - {item.Quantity} x {item.Price:C}</li>");
                    }
            
                    sb.AppendLine("</ul>");
                    sb.AppendLine($"<p><strong>Not:</strong> {note}</p>");
            
                    mimeMessage.Body = new TextPart("html")
                    {
                        Text = sb.ToString()
                    };

                using var smtp = new SmtpClient
                {
                        Timeout = 10000 // Timeout in milliseconds (10 seconds)
                };

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect), SecureSocketOptions.None);
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
