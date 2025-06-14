﻿using BirileriWebSitesi.Data;
using BirileriWebSitesi.Helpers;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.OrderAggregate;
using Iyzipay.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IInventoryService _inventoryService;
        public PaymentController(ILogger<CartController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager,
                                IProductService productService, IBasketService basketService,
                                IOrderService orderService, IEmailService emailService, IIyzipayPaymentService iyizpayService
                                , IUserAuditService userAuditService, IInventoryService inventoryService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _iyzipayService = iyizpayService;
            _userAuditService = userAuditService;
            _orderService = orderService;
            _basketService = basketService;
            _inventoryService = inventoryService;
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
                return RedirectToAction("NotFound","Home");
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
                    return Ok (new { success = false, message = "Sipariş Kaydedilirken Hata ile Karşılaşıldı." });
                    
                }

                string? buyerID = _userManager.GetUserId(User);
                
                UserAudit userAudit = await _userAuditService.GetUsurAuditAsync(buyerID);

                if (userAudit == null)
                    return Ok(new { success = false, message = "Kullanıcı Bilgileri Bulunamadı." });

                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                bool isProduction = environment == "Production";
                if (isProduction)
                {
                    if (string.IsNullOrEmpty(userAudit.Ip))
                        return Ok(new { success = false, message = "IP Bilgileri Bulunamadı." });
                    if (string.IsNullOrEmpty(userAudit.City))
                        return Ok(new { success = false, message = "Şehir Bilgileri Bulunamadı." });
                    if (string.IsNullOrEmpty(userAudit.Country))
                        return Ok(new { success = false, message = "Ülke Bilgileri Bulunamadı." });
                }
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
                if(!isProduction)
                {
                    model.Ip = "127.0.0.1";
                    model.City = "İzmir";
                    model.Country = "Türkiye";
                }
                else
                {
                    model.Ip = userAudit.Ip;
                    model.City = userAudit.City;
                    model.Country = userAudit.Country;
                }
                string resultString = string.Empty;
                //if credit card payment
                if (model.PaymentType == 1)
                {
                    if (!StringHelper.IsValidCVV(model.CVV))
                        return Ok(new { success = false, message = "Uygun Olmayan CVV Formatı." });
                    if (!StringHelper.IsValidExpiry(model.ExpMonth,model.ExpYear))
                        return Ok(new { success = false, message = "Uygun Olmayan Son Kullanma Tarihi Formatı." });
                    if (!StringHelper.IsValidCardNumber(model.CreditCardNumber))
                        return Ok(new { success = false, message = "Uygun Olmayan Kredi Kartı Formatı." });

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
                            await _orderService.UpdateOrderStatus(model.OrderId, "Approved", 1);
                            Order order = await _orderService.GetOrderAsync(model.OrderId);
                            await _inventoryService.UpdateInventoryAsync(order, 2);

                            if (!String.IsNullOrEmpty(model.EmailAddress))
                                await _emailService.SendPaymentEmailAsync(model.EmailAddress, model.OrderId, "CreditCard");

                            return Ok(new { success = true, is3Ds = false, message = "Siparişiniz İşleme Alındı." });
                        }
                        else
                        {
                            _logger.LogError(resultString);
                            return Ok(new { success = false, message = resultString });
                        }
                    }
                }
                //if bank transfer
                else
                {
                    await _basketService.DeleteBasketAsync(buyerID);
                    await _orderService.UpdateOrderStatus(model.OrderId, "Bank Transfer", model.PaymentType);
                    Order order = await _orderService.GetOrderAsync(model.OrderId);
                    await _inventoryService.UpdateInventoryAsync(order,2);
                    if(!String.IsNullOrEmpty(model.EmailAddress))
                        await _emailService.SendPaymentEmailAsync(model.EmailAddress,model.OrderId,"BankAccount");
                    return Ok(new { success = true, is3Ds = false, message = "Banka Ödemesi ile Ödeme Tanımlanmıştır." });
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
                var paymentLogOrder = await _context.Orders.FindAsync(Convert.ToInt32(payment.BasketId));
                paymentLog.Order = paymentLogOrder;
                if (payment.Status == "success")
                {
                    TempData["SuccessMessage"] = "Siparişiniz Başarıyla İşleme Alındı.";
                    string? userId = await _context.Orders.Where(i => i.Id == Convert.ToInt32(payment.BasketId))
                                                          .Select(b=>b.BuyerId)
                                                          .FirstOrDefaultAsync();
                    if(!string.IsNullOrEmpty(userId))
                        await _basketService.DeleteBasketAsync(userId);
                    await _orderService.UpdateOrderStatus(Convert.ToInt32(payment.BasketId), "Approved",1);
                    await _orderService.RecordPayment(paymentLog);
                    Order order = await _orderService.GetOrderAsync(Convert.ToInt32(payment.BasketId));
                    await _inventoryService.UpdateInventoryAsync(order, 2);

                    string? mailAddress = await _context.Orders
                        .Where(i => i.Id == Convert.ToInt32(payment.BasketId))
                        .Include(b => b.BillingAddress)
                        .Select(b => b.BillingAddress.EmailAddress)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(mailAddress))
                        mailAddress = await _context.Orders
                             .Include(b => b.ShipToAddress)
                            .Select(b => b.ShipToAddress.EmailAddress)
                            .FirstOrDefaultAsync();
                    bool? customerMailSent = false;
                    if(!string.IsNullOrEmpty(mailAddress))
                        customerMailSent = await _emailService.SendPaymentEmailAsync(mailAddress, Convert.ToInt32(payment.BasketId), "CreditCard");
                    else
                    {
                        TempData["SuccessMessage"] = "Siparişiniz başarıyla alındı ancak e-posta adresi bulunamadığından mail gönderilemedi. " +
                                                    "Lütfen bizimle iletişim formundan iletişime geçiniz.";
                        _logger.LogWarning("Email address not found for order ID: " + payment.BasketId);
                        return LocalRedirect("/Identity/Account/Manage");
                    }

                    await _emailService.SendCustomerOrderMailAsync(paymentLogOrder);
                    TempData["SuccessMessage"] = "Ödemeniz alındı. Siparişiniz en geç 2 gün içerisinde hazırlanarak kargoya teslim edilecektir. ";
                    return LocalRedirect("/Identity/Account/Manage");
                }
                else
                {
                    _logger.LogError("Payment failed: " + payment.Status + DateTime.Now.ToString());
                    TempData["DangerMessage"] = "Siparişiniz oluşturulamadı. Lütfen daha sonra tekrar deneyiniz.";
                    await _orderService.UpdateOrderStatus(Convert.ToInt32(payment.BasketId), "Failed", 1);
                    
                    await _orderService.RecordPayment(paymentLog);
                    return RedirectToAction("Index","Home");
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

        [HttpGet]
        public IActionResult RedirectWithSuccess(int paymentType)
        {
            if (paymentType == 1)
                TempData["SuccessMessage"] = "Ödemeniz başarıyla alındı.";
            else
                TempData["SuccessMessage"] = "Siparişinizin kaydedildi. Siparişiniz banka havalesi gerçekleştikten sonra işleme alınacaktır.";
            return LocalRedirect("/Identity/Account/Manage");
        }
    }
}
