using BirileriWebSitesi.Data;
using BirileriWebSitesi.Helpers;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using Microsoft.AspNetCore.Mvc;

namespace BirileriWebSitesi.Controllers
{
    public class EmailController : Controller
    {
        private readonly ILogger<EmailController> _logger;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        public EmailController(ILogger<EmailController> logger, IEmailService emailService, ApplicationDbContext context)
        {
            _logger = logger;
            _emailService = emailService;
            _context = context;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Subscribe(string emailAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(emailAddress))
                    return Ok(new { success = false, message = "Email adresi boş olamaz." });

                // Validate email format
                if (!StringHelper.IsValidEmail(emailAddress))
                    return Ok(new { success = false, message = "Hatalı Email formatı." });

                if (_context.Subscribers.Any(s => s.EmailAddress == emailAddress))
                    return Ok(new { success = false, message = "Email abone listesinde mevcut." });

                // Save to the database
                var subscriber = new Subscriber { EmailAddress = emailAddress, SubscribedOn = DateTime.Now };
                _context.Subscribers.Add(subscriber);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Kayıt Başarılı!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Kayıt esnasında hata ile Karşılaşıldı. Lütfen daha sonra tekrar deneyiniz." });
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SendEmail(string username, string emailAddress, string phone, string message, string subject)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                    return Ok(new { success = false, message = "İsim boş olamaz." });
                if (string.IsNullOrEmpty(emailAddress))
                    return Ok(new { success = false, message = "Email adresi boş olamaz." });
                // Validate email format
                if (!StringHelper.IsValidEmail(emailAddress))
                    return Ok(new { success = false, message = "Hatalı Email formatı." });
                if (string.IsNullOrEmpty(message))
                    return Ok(new { success = false, message = "Mesaj boş olamaz." });
                if (string.IsNullOrEmpty(subject))
                    return Ok(new { success = false, message = "Konu boş olamaz." });

                string result = await _emailService.SendContactUsEmailAsync(username, emailAddress, phone, message, subject);
                if (result == "Mail Gönderildi")
                    return Ok(new { success = true, message = result });
                else
                    return Ok(new { success = false, message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return StatusCode(500, new { success = false, message = ex.Message.ToString() });
            }
        }
    }
}
