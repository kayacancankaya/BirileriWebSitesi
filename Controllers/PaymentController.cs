using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Services;
using Iyzipay.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BirileriWebSitesi.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBasketService _basketService;
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;
        private readonly IIyzipayPaymentService _iyzipayService;
        private readonly IUserAuditService _userAuditService;
        public PaymentController(ILogger<CartController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager,
                                IProductService productService, IBasketService basketService,
                                IOrderService orderService, IEmailService emailService, IIyzipayPaymentService iyizpayService
                                , IUserAuditService userAuditService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _iyzipayService = iyizpayService;
            _userAuditService = userAuditService;
            _orderService = orderService;
            _basketService = basketService;
        }
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var paymentJson = HttpContext.Session.GetString("PaymentViewModel");

                if (string.IsNullOrEmpty(paymentJson))
                    return RedirectToAction("Not Found"); // Or show a nice message

                var paymentModel = JsonConvert.DeserializeObject<PaymentViewModel>(paymentJson);
                HttpContext.Session.Remove("PaymentViewModel");
                return View(paymentModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }

        }
        [ValidateAntiForgeryToken]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PaymentRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToList();
                    _logger.LogError("ModelState is not valid, PlaceOrder controller");
                    return BadRequest(new { success = false, message = "Sipariş Kaydedilirken Hata ile Karşılaşıldı.", errors });
                }

                string? buyerID = _userManager.GetUserId(User);


                UserAudit userAudit = await _userAuditService.GetUsurAuditAsync(buyerID);

                if (userAudit == null)
                    return Ok(new { success = false, message = "Kullanıcı Bilgileri Bulunamadı." });
                //if (string.IsNullOrEmpty(userAudit.Ip))
                //    return Ok(new { success = false, message = "IP Bilgileri Bulunamadı." });
                //if (string.IsNullOrEmpty(userAudit.City))
                //    return Ok(new { success = false, message = "Şehir Bilgileri Bulunamadı." });
                //if (string.IsNullOrEmpty(userAudit.Country))
                //    return Ok(new { success = false, message = "Ülke Bilgileri Bulunamadı." });
                if (userAudit.RegistrationDate == null)
                    return Ok(new { success = false, message = "Kayıt Tarihi Bilgileri Bulunamadı." });
                if (userAudit.LastLoginDate == null)
                    return Ok(new { success = false, message = "Son Giriş Tarihi Bilgileri Bulunamadı." });
                DateTime lastLoginDate;
                DateTime registrationDate;
                if (!DateTime.TryParse(userAudit.LastLoginDate.ToString(), out lastLoginDate))
                    return Ok(new { success = false, message = "Son Giriş Tarihi Bilgileri Bulunamadı." });
                if (!DateTime.TryParse(userAudit.RegistrationDate.ToString(), out registrationDate))
                    return Ok(new { success = false, message = "Kayıt Tarihi Bilgileri Bulunamadı." });

                model.RegistrationDate = lastLoginDate;
                model.LastLoginDate = registrationDate;
                model.Ip = "127.0.0.1";
                model.City = "İzmir";
                model.Country = "Türkiye";
                string resultString = string.Empty;
                if (model.PaymentType == 1)
                {
                    if (model.Force3Ds)
                    {

                        resultString = await _orderService.Process3DsOrderAsync(model);

                        if (resultString.TrimStart().StartsWith("<!doctype html>", StringComparison.OrdinalIgnoreCase))
                            return Ok(new { success = true, is3Ds = true, htmlContent = resultString, message = "Yönlendiriliyor..." });

                        return Ok(new { success = false, message = resultString });

                    }
                    else
                    {
                        resultString = await _orderService.ProcessOrderAsync(model);

                        if (resultString == "success")
                        {
                            await _basketService.DeleteBasketAsync(buyerID);
                            return Ok(new { success = true, is3Ds = true, message = "Siparişiniz İşleme Alındı." });
                        }
                        else
                        {
                            _logger.LogError(resultString);
                            return Ok(new { success = false, message = resultString });
                        }
                    }
                }
                else
                {
                    return Ok(new { success = true, is3Ds = false, message = "Banka Transferi" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Ok(new { success = false, message = "Sipariş Kaydedilirken Hata ile Karşılaşıldı. Lütfen Tekrar Deneyiniz." });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Payment3dsCallBack()
        {
            try
            {
                ThreedsPayment payment = await _iyzipayService.Payment3dsCallBack(Request.Form["conversationId"], Request.Form["paymentId"]);


                PaymentLog paymentLog = new();
                paymentLog.ConversationId = payment.ConversationId;
                paymentLog.OrderId = Convert.ToInt32(payment.BasketId);
                paymentLog.PaymentId = payment.PaymentId;
                paymentLog.Price = payment.Price;
                paymentLog.PaidPrice = payment.PaidPrice;
                paymentLog.IyziCommissionRateAmount = payment.IyziCommissionRateAmount;
                paymentLog.IyziCommissionFee = payment.IyziCommissionFee;
                paymentLog.CardFamily = payment.CardFamily;
                paymentLog.CardAssociation = payment.CardAssociation;
                paymentLog.CardType = payment.CardType;
                paymentLog.BinNumber = payment.BinNumber;
                paymentLog.LastFourDigits = payment.LastFourDigits;
                paymentLog.Status = payment.Status;
                paymentLog.PaidAt = DateTime.UtcNow;



                if (payment.Status == "success")
                {
                    TempData["SuccessMessage"] = "Siparişiniz Başarıyla İşleme Alındı.";

                    await _basketService.DeleteBasketAsync(Convert.ToInt32(payment.BasketId));
                    await _orderService.UpdateOrderStatus(Convert.ToInt32(payment.BasketId), "Approved");
                    await _orderService.RecordPayment(paymentLog);
                    // await _emailService.SendOrderConfirmationEmailAsync(_userManager.GetUserId(User));
                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.LogError("Payment failed: " + payment.Status + DateTime.Now.ToString());
                    TempData["DangerMessage"] = payment.Status.ToString();
                    await _orderService.UpdateOrderStatus(Convert.ToInt32(payment.BasketId), "Failed");
                    await _orderService.RecordPayment(paymentLog);
                    return RedirectToAction("Index");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CheckInstallment([FromBody] BinRequestDTO model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.BinNumber))
                    return BadRequest(new { success = false, message = "Kart Numarası Boş Olamaz." });

                InstallmentDetail installmentInfo = await _orderService.GetInstallmentInfoAsync(model.BinNumber, model.Price);
                if (installmentInfo == null)
                    return StatusCode(500, new { success = false, message = "Hata ile Karşılaşıldı. Lütfen Tekrar Deneyiniz." });

                return Ok(new { installments = installmentInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return StatusCode(500, new { success = false, message = "Hata ile Karşılaşıldı. Lütfen Tekrar Deneyiniz." });
            }
        }
    }
}
