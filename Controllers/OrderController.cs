using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using BirileriWebSitesi.Data;
using BirileriWebSitesi.Helpers;
using BirileriWebSitesi.Models.OrderAggregate;
using BirileriWebSitesi.Models.InquiryAggregate;

namespace BirileriWebSitesi.Controllers
{
    public class OrderController : Controller
    {

        private readonly ILogger<CartController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IProductService _productService;
        private readonly IBasketService _basketService;
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;
        private readonly IIyzipayPaymentService _iyizpayService;
        private readonly IUserAuditService _userAuditService;
        public OrderController(ILogger<CartController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager,
                                IProductService productService, IBasketService basketService, IOrderService orderService,
                                IUserAuditService userAuditService, IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _productService = productService;
            _basketService = basketService;
            _orderService = orderService;
            _userAuditService = userAuditService;
            _emailService = emailService;
        }
        [Authorize]
        public async Task<IActionResult> CheckOut()
        {
            try
            {
                string? userID = string.Empty;
                string? email = string.Empty;
                string? firstName = string.Empty;
                string? lastName = string.Empty;
                string? fullName = string.Empty;
                string? phone = string.Empty;

                IdentityUser? user = new();

                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                    user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                }
                else
                    return RedirectToAction("NotFound", "Home");
                bool isInBuyRegion = false;
                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                bool isProduction = environment == "Production";
                if (isProduction)
                {
                    string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    isInBuyRegion = await _userAuditService.IsInBuyRegion(userID, ip);

                    if (!isInBuyRegion)
                    {
                        TempData["WarningMessage"] = "Hizmetimiz Türkiye sınırları içinde geçerlidir.";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                    isInBuyRegion = true;

                Basket basket = await _basketService.GetBasketAsync(userID);
                List<Models.OrderAggregate.OrderItem> orderItems = new();
                foreach (Models.BasketAggregate.BasketItem item in basket.Items)
                {
                    Models.OrderAggregate.OrderItem orderItem = new(item.ProductCode, item.Quantity, item.UnitPrice, item.ProductName);

                    orderItems.Add(orderItem);
                }

                Models.OrderAggregate.Address? shipToAddress = await _context.Addresses.Where(i => i.UserId == userID &&
                                                                                                   i.IsBilling == false &&
                                                                                                   i.SetAsDefault == true)
                                                                                        .OrderByDescending(i => i.Id)
                                                                                        .FirstOrDefaultAsync();

                if (shipToAddress == null)
                {
                    email = await _userManager.GetEmailAsync(user);
                    phone = await _userManager.GetPhoneNumberAsync(user);
                    fullName = await _userManager.GetUserNameAsync(user);
                    firstName = StringHelper.GetFirstName(fullName);
                    lastName = StringHelper.GetLastName(fullName);
                    shipToAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, true, string.Empty, string.Empty, firstName, lastName, email, phone, false, string.Empty);
                }

                Models.OrderAggregate.Address? billingAddress = await _context.Addresses.Where(i => i.UserId == userID &&
                                                                                                      i.IsBilling == true &&
                                                                                                    i.SetAsDefault == true)
                                                                                        .OrderByDescending(i => i.Id)
                                                                                        .FirstOrDefaultAsync();

                if (billingAddress == null)
                {
                    email = await _userManager.GetEmailAsync(user);
                    phone = await _userManager.GetPhoneNumberAsync(user);
                    fullName = await _userManager.GetUserNameAsync(user);
                    firstName = StringHelper.GetFirstName(fullName);
                    lastName = StringHelper.GetLastName(fullName);
                    billingAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, string.Empty, string.Empty, firstName, lastName, email, phone, false, string.Empty);

                }

                Order order = new(userID, shipToAddress, billingAddress, orderItems, isInBuyRegion, true, 1);
                if (order.TotalAmount > 100000)
                {
                    TempData["DangerMessage"] = "Sepet Miktarı 100000₺'den Büyük Olamaz.";
                    return RedirectToAction("Index", "Cart");
                }


                return View(order);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return RedirectToAction("NotFound", "Home");
            }
        }
        public IActionResult _PartialIsCorporate(Models.OrderAggregate.Address address)
        {
            try
            {
                if (ModelState.IsValid)
                    return PartialView(address);

                Models.OrderAggregate.Address emptyAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
                return PartialView(address);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                Models.OrderAggregate.Address emptyAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
                return PartialView(emptyAddress);
            }
        }
        [ValidateAntiForgeryToken]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProceedToPayment([FromBody] OrderRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToList();
                    return BadRequest(new { success = false, message = "Sipariş Oluşturulurken Hata İle Karşılaşıldı.", errors });
                }

                string? buyerID = _userManager.GetUserId(User);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (ip == "::1")
                    ip = "212.252.136.146";


                if (string.IsNullOrEmpty(buyerID))
                    return BadRequest(new { success = false, message = "Kullanıcı Bulunamadı." });

                if (model.ShipToAddress == null)
                    return BadRequest(new { success = false, message = "Gönderi Bilgilier Bulunamadı." });
                if (model.BillingAddress == null)
                    return BadRequest(new { success = false, message = "Fatura Bilgileri Bulunamadı." });
                if (model.OrderItems == null)
                    return BadRequest(new { success = false, message = "Ürün Bilgileri Bulunamadı." });
                if (model.OrderItems.Count() <= 0)
                    return BadRequest(new { success = false, message = "Ürün Bilgileri Bulunamadı." });
                if (string.IsNullOrEmpty(model.Notes))
                    model.Notes = string.Empty;
                Models.OrderAggregate.Address ShipToAddress = new();
                ShipToAddress.UserId = buyerID;
                ShipToAddress.FirstName = model.ShipToAddress.FirstName;
                ShipToAddress.LastName = model.ShipToAddress.LastName;
                ShipToAddress.CorporateName = model.ShipToAddress.CorporateName;
                ShipToAddress.EmailAddress = model.ShipToAddress.EmailAddress;
                ShipToAddress.Phone = model.ShipToAddress.Phone;
                ShipToAddress.AddressDetailed = model.ShipToAddress.AddressDetailed;
                ShipToAddress.Street = model.ShipToAddress.Street;
                ShipToAddress.City = model.ShipToAddress.City;
                ShipToAddress.State = model.ShipToAddress.State;
                ShipToAddress.Country = model.ShipToAddress.Country;
                ShipToAddress.ZipCode = model.ShipToAddress.ZipCode;
                ShipToAddress.IsBilling = model.ShipToAddress.IsBilling;
                ShipToAddress.IsBillingSame = (bool)(model.ShipToAddress.IsBillingSame == null ? false : model.ShipToAddress.IsBillingSame);
                Models.OrderAggregate.Address BillingAddress = new();
                BillingAddress.UserId = buyerID;
                BillingAddress.FirstName = model.BillingAddress.FirstName;
                BillingAddress.LastName = model.BillingAddress.LastName;
                BillingAddress.CorporateName = model.BillingAddress.CorporateName;
                BillingAddress.EmailAddress = model.BillingAddress.EmailAddress;
                BillingAddress.Phone = model.BillingAddress.Phone;
                BillingAddress.AddressDetailed = model.BillingAddress.AddressDetailed;
                BillingAddress.Street = model.BillingAddress.Street;
                BillingAddress.City = model.BillingAddress.City;
                BillingAddress.State = model.BillingAddress.State;
                BillingAddress.Country = model.BillingAddress.Country;
                BillingAddress.ZipCode = model.BillingAddress.ZipCode;
                BillingAddress.IsBilling = model.BillingAddress.IsBilling;
                BillingAddress.IsBillingSame = (bool)(model.BillingAddress.IsBillingSame == null ? false : model.BillingAddress.IsBillingSame);
                BillingAddress.CorporateName = model.BillingAddress.CorporateName;
                BillingAddress.VATnumber = model.BillingAddress.VATnumber;
                BillingAddress.VATstate = model.BillingAddress.VATstate;
                BillingAddress.IsCorporate = (bool)(model.BillingAddress.IsCorporate == null ? false : model.BillingAddress.IsCorporate);

                List<Models.OrderAggregate.OrderItem> orderItems = new();
                decimal totalAmount = 0;
                foreach (var item in model.OrderItems)
                {
                    Models.OrderAggregate.OrderItem orderItem = new(item.ProductCode, item.Units, item.UnitPrice, item.ProductName);
                    totalAmount += (item.Units * item.UnitPrice);
                    orderItems.Add(orderItem);
                }

                //save order info
                Order order = new(buyerID, ShipToAddress, BillingAddress, orderItems, true, true, 1);

                string orderResult = await _orderService.SaveOrderInfoAsync(order);
                int orderID = 0;
                if (!Int32.TryParse(orderResult, out orderID))
                    return Ok(new { success = false, message = orderResult });
                if (orderID == 0 || orderID == -1)
                    return Ok(new { success = false, message = "Kargo ve Fatura Bilgileri Kaydedilirken Hata ile Karşılaşıldı. " });

                //prepare payment view model
                PaymentViewModel payment = new PaymentViewModel();
                payment.ShipToAddress = ShipToAddress;
                payment.BillingAddress = BillingAddress;
                payment.OrderItems = orderItems;
                payment.TotalAmount = totalAmount;
                payment.OrderId = orderID;

                HttpContext.Session.SetString("PaymentViewModel", JsonConvert.SerializeObject(payment));

                return Json(new { success = true, redirectUrl = Url.Action("Index", "Payment") });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Ok(new { success = false, message = "Kargo ve Fatura Bilgileri Kaydedilirken Hata ile Karşılaşıldı. " });
            }
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

        [ValidateAntiForgeryToken]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CheckOutInquiry()
        {
            try
            {

                string? userID = string.Empty;
                string? email = string.Empty;
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                    IdentityUser user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                    email = await _userManager.GetEmailAsync(user);
                }
                else
                    return Ok(new { success = false, message = "Kullanıcı Bulunamadı." });

                Inquiry inquiry = await _basketService.GetInquiryBasketAsync(userID);

                bool result = await _emailService.SendInquiryEmailAsync(email, inquiry);
                if (result)
                {
                    TempData["SuccessMessage"] = "Talebiniz Alındı. En kısa sürede sizinle iletişime geçilecektir.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["DangerMessage"] = "Talebiniz Alınırken Hata ile Karşılaşıldı. Lütfen daha sonra tekrar deneyiniz.";
                    return RedirectToAction("Inquiry", "Cart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Ok(new { success = false, message = "İstek Gönderilirken Hata ile Karşılaşıldı." });
            }
        }


        [ValidateAntiForgeryToken]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelOrderAsync(int orderId)
        {
            try
            {
                int resultInt = await _orderService.CancelOrderAsync(orderId);
                string userID = string.Empty;
                IdentityUser? user = new();

                if (!User.Identity.IsAuthenticated)
                {
                    TempData["DangerMessage"] = "Kullanıcı Bulunamadı." ;
                    return Redirect("/Identity/Account/Manage/Orders");
                }
                userID = _userManager.GetUserId(User);
                user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                string email = user.Email;
                Order order = await _orderService.GetOrderAsync(orderId);
                if (resultInt == -1)
                {
                    TempData["DangerMessage"] = "Sipariş İptal Edilirken Hata ile Karşılaşıldı. \n" +
                                                 "Lütfen İletişim Kutusundan Bizimle İletişime Geçiniz.";
                }
                else if (resultInt == 0)
                {
                    TempData["DangerMessage"] = "İlgili Sipariş Sistemde Bulunamadı. \n" +
                                                 "Lütfen İletişim Kutusundan Bizimle İletişime Geçiniz.";
                }
                else if (resultInt == 1)
                {
                    TempData["SuccessMessage"] = "Siparişiniz İptal Edildi.";
                    await _emailService.SendOrderCancelledEmailAsync(order,email, "Products Not Recieved" );
                }
                else if (resultInt == 2)
                {
                    TempData["SuccessMessage"] = "Sipariş İptal Talebiniz Alındı.\nÜrünler Tarafımıza Ulaştığında Ücret İadesi Sağlanacaktır.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Recieved");
                }
                else if (resultInt == 3)
                {
                    TempData["DangerMessage"] = "Ödeme iade edilirken hata ile karşılaşıldı. \n" +
                                                 "Lütfen İletişim Kutusundan Bizimle İletişime Geçiniz.";
                }

                else if (resultInt == 4)
                {
                    TempData["SuccessMessage"] = "Siparişiniz İptal Edildi.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Not Recieved");
                }

                else if (resultInt == 5)
                {
                    TempData["SuccessMessage"] = "Sipariş İptal Talebiniz Alındı.\nÜrünler Tarafımıza Ulaştığında Ücret İadesi Sağlanacaktır.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Recieved");
                }
                return Redirect("/Identity/Account/Manage/Orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Ok(new { success = false, message = "Sepet Temizlenirken Hata ile Karşılaşıldı." });
            }
        }
    }
}
