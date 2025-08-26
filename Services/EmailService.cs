using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.OrderAggregate;
using System.Text;
using BirileriWebSitesi.Models.InquiryAggregate;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static BirileriWebSitesi.Models.Enums.AprrovalStatus;
using BirileriWebSitesi.Helpers;

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
        public async Task<bool> SendPaymentEmailAsync(string to, int orderID,string shipmentCompany, string paymentType)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                email.To.Add(MailboxAddress.Parse(to));
                string htmlMessage = string.Empty;

                string cargo = string.IsNullOrEmpty(shipmentCompany) ? string.Empty : $"<span style='color: green;font-weight:bold;'>{shipmentCompany}</span> firmasına";
                if (paymentType=="CreditCard")
                {
                    string subject = $"{orderID} - Ödeme Alındı";
                    email.Subject = subject;
                    htmlMessage = $@"
                    <table style='max-width:600px;margin:auto;font-family:Arial,sans-serif;border:1px solid #ddd;border-radius:8px;padding:20px;'>
                        <tr>
                            <td style='text-align:center;'>
                                <h2 style='color:#007bff;'>Ödemeniz Alındı</h2>
                                <p style='font-size:16px;'>Sipariş Numaranız: <strong>{orderID}</strong></p>
                                <p style='font-size:16px;'>Ödemeniz başarıyla alınmıştır. Siparişiniz en geç 2 iş günü içinde hazırlanarak {cargo} teslim edilecektir.</p>
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
                                <p style='font-size:16px;'>Form gönderildikten sonra siparişiniz işleme alınacak ve en geç 2 iş günü içinde {cargo} teslim edilecektir.</p><br />
                                 <span><strong>Banka Adı:</strong> T. Garanti Bankası A.Ş.</span><br />
                                <span><strong>Şube:</strong> KARŞIYAKA ÇARŞI </span><br />
                                <span><strong>Hesap No:</strong> 1596-6298952</span><br />
                                <span><strong>Alıcı:</strong> BİRİLERİ DIŞ TİCARET DANIŞMANLIK SANAYİ VE TİCARET LİMİTED Ş.</span><br />
                                <span><strong>IBAN:</strong> TR89 0006 2001 5960 0006 2989 52</span><br />

                            </td>
                        </tr>
                    </table>";
                }

                var builder = new BodyBuilder { HtmlBody = htmlMessage };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                smtp.Timeout = 30000; // Timeout in milliseconds (10 seconds)
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
        public async Task<bool> SendBankTransferNoticeEmailAsync(Order order, string note)
        {
            try
            {
                
                string billingName = string.Empty;

                if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                    billingName = string.Format("{0} {1}",order.BillingAddress.FirstName,order.BillingAddress.LastName);
                else
                    billingName = order.BillingAddress.CorporateName;
                
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                mimeMessage.To.Add(MailboxAddress.Parse(_configuration["SMTP:InfoAddress"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC1"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC2"]));

                mimeMessage.Subject = $"{order.Id} Sipariş Havale Formu";

                    // Build HTML content
                    var sb = new StringBuilder();
                    sb.AppendLine("<h3>Sipariş Bilgileri:</h3><ul>");
            
                    foreach (var item in order.OrderItems)
                    {
                        sb.AppendLine($"<li>{item.ProductName} - {item.Units} x {item.UnitPrice:C}</li>");
                    }
            
                    sb.AppendLine("</ul>");
                    // Billing Address
                    sb.AppendLine("<h3>Fatura Adresi</h3>");
                    sb.AppendLine("<p>");
                    sb.AppendLine($"{billingName}<br>");
                    sb.AppendLine($"Vergi No: {order.BillingAddress?.VATnumber}<br>");
                    sb.AppendLine($"{order.BillingAddress?.Street}<br>");
                    sb.AppendLine($"{order.BillingAddress?.City}, {order.BillingAddress?.State} {order.BillingAddress?.ZipCode}<br>");
                    sb.AppendLine($"{order.BillingAddress?.AddressDetailed}<br>");
                    sb.AppendLine("</p>");
            
                    // Shipping Address
                    sb.AppendLine("<h3>Teslimat Adresi</h3>");
                    sb.AppendLine("<p>");
                    sb.AppendLine($"{order.ShipToAddress?.FirstName} {order.ShipToAddress?.LastName}<br>");
                    sb.AppendLine($"{order.ShipToAddress?.Street}<br>");
                    sb.AppendLine($"{order.ShipToAddress?.City}, {order.ShipToAddress?.State} {order.ShipToAddress?.ZipCode}<br>");
                    sb.AppendLine($"{order.ShipToAddress?.AddressDetailed}<br>");
                    sb.AppendLine("</p>");
            
                    // Note
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        sb.AppendLine("<h3>Ek Not</h3>");
                        sb.AppendLine($"<p>{note}</p>");
                    }
            
                    mimeMessage.Body = new TextPart("html")
                    {
                        Text = sb.ToString()
                    };
                     sb.AppendLine("</ul>");

                using var smtp = new SmtpClient
                {
                        Timeout = 10000 // Timeout in milliseconds (10 seconds)
                };

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(mimeMessage);
                await smtp.DisconnectAsync(true);

                return true;

                
            }
            catch (Exception)
            {
                return false;
                
            }
        }
        public async Task<bool> SendCustomerOrderMailAsync(Order order)
        {
            try
            {
                string billingName = string.Empty;

                if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                    billingName = string.Format("{0} {1}",order.BillingAddress.FirstName,order.BillingAddress.LastName);
                else
                    billingName = order.BillingAddress.CorporateName;
                    
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                mimeMessage.To.Add(MailboxAddress.Parse(_configuration["SMTP:InfoAddress"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC1"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC2"]));

                mimeMessage.Subject = $"{order.Id} Sipariş Geldi";

                // Build HTML content
                var sb = new StringBuilder();
                sb.AppendLine("<h3>Sipariş Bilgileri:</h3><ul>");

                foreach (var item in order.OrderItems)
                {
                    sb.AppendLine($"<li>{item.ProductName} - {item.Units} x {item.UnitPrice:C}</li>");
                }

                sb.AppendLine("</ul>");
                // Billing Address
                sb.AppendLine("<h3>Fatura Adresi</h3>");
                sb.AppendLine("<p>");
                sb.AppendLine($"{billingName}<br>");
                sb.AppendLine($"Vergi No: {order.BillingAddress?.VATnumber}<br>");
                sb.AppendLine($"{order.BillingAddress?.Street}<br>");
                sb.AppendLine($"{order.BillingAddress?.City}, {order.BillingAddress?.State} {order.BillingAddress?.ZipCode}<br>");
                sb.AppendLine($"{order.BillingAddress?.AddressDetailed}<br>");
                sb.AppendLine("</p>");

                // Shipping Address
                sb.AppendLine("<h3>Teslimat Adresi</h3>");
                sb.AppendLine("<p>");
                sb.AppendLine($"{order.ShipToAddress?.FirstName} {order.ShipToAddress?.LastName}<br>");
                sb.AppendLine($"{order.ShipToAddress?.Street}<br>");
                sb.AppendLine($"{order.ShipToAddress?.City}, {order.ShipToAddress?.State} {order.ShipToAddress?.ZipCode}<br>");
                sb.AppendLine($"{order.ShipToAddress?.AddressDetailed}<br>");
                sb.AppendLine("</p>");

                // Note
                if (!string.IsNullOrWhiteSpace(order.AdditionalNotes))
                {
                    sb.AppendLine("<h3>Ek Not</h3>");
                    sb.AppendLine($"<p>{order.AdditionalNotes}</p>");
                }

                mimeMessage.Body = new TextPart("html")
                {
                    Text = sb.ToString()
                };
                sb.AppendLine("</ul>");

                using var smtp = new SmtpClient
                {
                    Timeout = 10000 // Timeout in milliseconds (10 seconds)
                };

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(mimeMessage);
                await smtp.DisconnectAsync(true);

                return true;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş e-postası gönderilirken hata oluştu: {Message}", ex.Message);
                return false;

            }
        }
        public async Task<bool> SendInquiryEmailAsync(string email, Inquiry inquiry)
        {
            try
            {

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                mimeMessage.To.Add(MailboxAddress.Parse(_configuration["SMTP:InfoAddress"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC1"]));
                mimeMessage.Cc.Add(MailboxAddress.Parse(_configuration["SMTP:CC2"]));

                mimeMessage.Subject = $"Toptan Talep";

                // Build HTML content
                var sb = new StringBuilder();
                sb.AppendLine("<h3>İstek Bilgileri:</h3><ul>");

                sb.AppendLine($"Mail Adresi: {email}<br>");

                foreach (var item in inquiry.Items)
                {
                    sb.AppendLine($"<li>{item.ProductName} - {item.Quantity} x {item.UnitPrice:C}</li>");
                }

                sb.AppendLine("</ul>");

                mimeMessage.Body = new TextPart("html")
                {
                    Text = sb.ToString()
                };

                using var smtp = new SmtpClient
                {
                    Timeout = 10000 // Timeout in milliseconds (10 seconds)
                };

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(mimeMessage);
                await smtp.DisconnectAsync(true);

                return true;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toptan İstek e-postası gönderilirken hata oluştu: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> SendOrderCancelledEmailAsync(Order order, string userEmail, string type)
        {
            try
            {
                string html = string.Empty;
                string subject = type == "Products Recieved" ? $"Sipariş İptal Talebiniz Alındı - {order.Id}" : $"Siparişiniz İptal Edildi - {order.Id}";
                string subjectAdmin = type == "Products Recieved" ? $"Sipariş İptal Talebi - {order.Id}" : $"Sipariş İptali - {order.Id}";
                string statusText = ((ApprovalStatus)order.Status).ToString();
                statusText = StringHelper.ConvertToTurkishStatus(statusText);
                string paymentType = order.PaymentType == 1 ? "Kredit Kartı" : "Hesaba Havale"; 

                // Build HTML content
                var sb = new StringBuilder();
                sb.AppendLine("<h3>Sipariş Bilgileri:</h3>");

                sb.AppendLine($"<p>Durumu: {statusText} </p></br>");
                sb.AppendLine($"<p>Ödeme Şekli: {paymentType} </p></br>");
                if (type == "Products Recieved")
                   sb.AppendLine($"<p>Ödeminiz siparişler tarafımıza ulaştığında yapılacaktır.</p></br>");
                sb.Append("<ul>");
                foreach (var item in order.OrderItems)
                    sb.AppendLine($"<li>{item.ProductName} - {item.Units} x {item.UnitPrice:C}</li>");
                sb.AppendLine("</ul>");

                html = sb.ToString();
               //send email to user
               await SendEmail(userEmail, null, null,subject, html);
               //send email to admin
               await SendEmail(_configuration["SMTP:InfoAddress"], _configuration["SMTP:CC1"], _configuration["SMTP:CC2"],
                                subjectAdmin, html);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilirken hata oluştu: {Message}", ex.Message);
                return false;
            }
        }
        public async Task<bool> SendOrderItemCancelledEmailAsync(Order order,OrderItem item, string userEmail, string type)
        {
            try
            {
                string html = string.Empty;
                string subject = type == "Products Recieved" ? $"Ürün İptal Talebiniz Alındı - {order.Id}" : $"Ürününüz İptal Edildi - {order.Id}";
                string subjectAdmin = type == "Products Recieved" ? $"Ürün İptal Talebi - {order.Id}" : $"Ürün İptali - {order.Id}";
                string statusText = item.IsRefunded ? "Geri Ödeme Yapıldı" : "Geri Ödeme Yapılmadı";
                string paymentType = order.PaymentType == 1 ? "Kredit Kartı" : "Hesaba Havale"; 

                // Build HTML content
                var sb = new StringBuilder();
                sb.AppendLine("<h3>Ürün Bilgileri:</h3>");

                sb.AppendLine($"<p>Ödeme Şekli: {paymentType} </p></br>");
                sb.AppendLine($"<p>Durumu: {statusText} </p></br>");
                if (type == "Products Recieved")
                   sb.AppendLine($"<p>Ödeminiz siparişler tarafımıza ulaştığında yapılacaktır.</p></br>");
                sb.Append("<ul>");
                sb.AppendLine($"<li>{item.ProductName} - {item.Units} x {item.UnitPrice:C}</li>");
                sb.AppendLine("</ul>");

                html = sb.ToString();
               //send email to user
               await SendEmail(userEmail, null, null,subject, html);
               //send email to admin
               await SendEmail(_configuration["SMTP:InfoAddress"], _configuration["SMTP:CC1"], _configuration["SMTP:CC2"],
                                subjectAdmin, html);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilirken hata oluştu: {Message}", ex.Message);
                return false;
            }
        }

        private async Task<bool> SendEmail(string to, string? cc1, string?cc2,string subject,
                                string html)
        {
            try
            {

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Birileri", _configuration["SMTP:Username"]));
                mimeMessage.To.Add(MailboxAddress.Parse(to));
                if (!string.IsNullOrEmpty(cc1))
                    mimeMessage.Cc.Add(MailboxAddress.Parse(cc1));
                if (!string.IsNullOrEmpty(cc2))
                    mimeMessage.Cc.Add(MailboxAddress.Parse(cc2));

                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart("html")
                {
                    Text = html
                };


                using var smtp = new SmtpClient
                {
                    Timeout = 10000 // Timeout in milliseconds (10 seconds)
                };

                await smtp.ConnectAsync("mail.kurumsaleposta.com", 465, SecureSocketOptions.SslOnConnect);
                await smtp.AuthenticateAsync(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);
                await smtp.SendAsync(mimeMessage);
                await smtp.DisconnectAsync(true);

                return true;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toptan İstek e-postası gönderilirken hata oluştu: {Message}", ex.Message);
                throw;
            }
        }

    }

}
