using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.OrderAggregate;
using BirileriWebSitesi.Models.ViewModels;
using Iyzipay.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
namespace BirileriWebSitesi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
       
        public HomeController( ILogger<HomeController> logger )
        {
            _logger = logger;
        }
        
        public IActionResult Index()
        {
            try
            {
                //authCookie = Request.Cookies[".AspNetCore.Identity.Application"];
                //if (authCookie != null)
                //    TempData["Cookie"] = "exists";
                //else
                //    TempData["Cookie"] = "not exists";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult _CatalogPartial()
        {
            try
            {
                List<string> Catalogs = new List<string> {
                   "Spor Malzemeleri",
                   "Ofis ve Kırtasiye Ürünleri",
                   "Pet Shop Ürünleri",
                   "Ev Gereçleri",
                   "Elektronik Ürünler",
                   "Oyuncak & Hobi Ürünleri"
                };
                return PartialView("_CatalogPartial", Catalogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest();
            }
        }
        public IActionResult OpenPdf(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return View("NotFound");

                filePath = Uri.UnescapeDataString(filePath);

                string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
                string fullPath = Path.Combine(rootPath, Path.GetFileName(filePath)); // Prevent directory traversal

                if (!System.IO.File.Exists(fullPath))
                    return View("NotFound");

                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                return File(stream, "application/pdf");

            }
            catch (Exception)
            {

                return View("NotFound");
            }
        }
        public IActionResult _PartialEcommerceService()
        {
            try
            {
                return PartialView("_PartialEcommerceService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest();
            }
        }
        public IActionResult Privacy()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult CookiePolicy()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult DistanceSellingAgreement()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult KVKK()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult About()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult _PartialContactUs()
        {
            try
            {
                return PartialView("_PartialContactUs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest();
            }
        }

        public IActionResult ContactUs()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult NotFound()
        {
            return View();
        }

        public async Task<IActionResult> Cart()
        {
            try
            {
                string? userID = string.Empty;
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var _productService = _serviceProvider.GetRequiredService<IProductService>();
                var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }
                if (string.IsNullOrEmpty(userID))
                {
                    string? cart = Request.Cookies["MyCart"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        Basket emptyBasket = new("0");
                        return View(emptyBasket);
                    }
                    Dictionary<string,int> productInfo = GetProductsFromCookie(cart);
                    Basket anonymousBasket = new("0");
                    string productCode = string.Empty;
                    int quantity = 0;
                    decimal unitPrice = decimal.Zero;
                    decimal totalAmount = 0;
                    foreach (var product in productInfo)
                    {
                        productCode = product.Key;
                        quantity = product.Value;
                        unitPrice = await _productService.GetPriceAsync(productCode);
                        await _basketService.AddItemToAnonymousBasketAsync(anonymousBasket,productCode, unitPrice,quantity);

                    }

                    return View(anonymousBasket);
                }

                Basket basket = await _basketService.GetBasketAsync(userID);
                return View(basket);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message.ToString());
                return View("NotFound");
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddToCart(string userId, string productCode, decimal price, int quantity)
        {
            try
            {
                if(price * quantity > 100000)
                {
                    TempData["DangerMessage"] = "Sepet Miktarı 100.000₺'den Büyük Olamaz.";
                    return Ok( new { success = false, message = "Sepet Miktarı 100.000₺'den Büyük Olamaz." });
                }

                string totalProduct = string.Empty;
                Dictionary<int, string> result = new();
                if (string.IsNullOrEmpty(productCode) ||
                    price <= 0 ||
                    quantity <= 0)
                    return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });

                //cookie 
                if (userId == "0")
                {
                    result = AddBasketCookie(productCode, quantity);
                    if(result.Values.FirstOrDefault() == "HATA")
                    {
                        TempData["TotalProduct"] = 0;
                        return Ok( new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });
                    }
                    totalProduct = result.Keys.FirstOrDefault().ToString();
                    return Ok(new { success=true, message = "Ürün Sepete Eklendi", totalProduct });
                }
                var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
                //db
                result =  await _basketService.AddItemToBasketAsync(userId, productCode, price, quantity);
                if (result.Values.FirstOrDefault() == "Ürün Sepete Eklenirken Hata ile Karşılaşıldı")
                {
                    return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });
                }
                totalProduct = result.Keys.FirstOrDefault().ToString();

                return Ok(new { success = true, message = "Ürün Sepete Eklendi", TotalProduct = totalProduct });
            }
            catch
            {
                return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });

            }
        }
        [HttpPost]
        public async Task<IActionResult> InitializeCartNumber()
        {
            try
            {
                string? userID = string.Empty;
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyCart"];
                    if(string.IsNullOrEmpty(cart))
                    {
                        message = "0";
                        return Ok(new { message });

                    }

                    string[] cartArray = cart.Split('&');
                    message = cartArray.Count().ToString();
                    return Ok(new { message });
                }

                //db
                int result = 0;
                var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
                result =  await _basketService.CountDistinctBasketItems(userID);
                message = result.ToString();
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                string message = "0";
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest(new { message });
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CartItemAmountChanged(string productCode,int quantity)
        {
            try
            {
                string? userID = string.Empty;
                Dictionary<int, string> result = new();
                int totalProductCount = 0;
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var _productService = _serviceProvider.GetRequiredService<IProductService>();
                var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyCart"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        message = "Sepet Bulunamadı";
                        return BadRequest(new { message });
                    }

                    result = UpdateCookie(productCode, quantity, cart);
                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault());

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                        return BadRequest(new { message });
                    }
                    TempData["message"] = message;
                    TempData["message"] = totalProductCount;
                    Dictionary<string, int> products = GetProductsFromCookie(result.Values.FirstOrDefault());
                    Basket cookieBasket = new("0");
                    foreach (var product in products)
                    {
                        decimal price = await _productService.GetPriceAsync(product.Key);
                        string productName = await _productService.GetProductNameAsync(product.Key);
                        string imagePath = await _productService.GetImagePathAsync(product.Key);
                        cookieBasket.AddItem(product.Key, price, product.Value,"0",productName,imagePath);
                    }
                    return PartialView("_PartialCart",cookieBasket); 

                }
                //db
                Basket basket = await _basketService.SetQuantity(userID,productCode,quantity);
                if (basket == null)
                    basket = new(userID);

                return PartialView("_PartialCart",basket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                string message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                return BadRequest(new { message });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RemoveCartItem(string productCode)
        {
            try
            {
                string? userID = string.Empty;
                Dictionary<int, string> result = new();
                int totalProductCount = 0;
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var _productService = _serviceProvider.GetRequiredService<IProductService>();
                var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyCart"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        message = "Sepet Bulunamadı";
                        return BadRequest(new { message });
                    }

                    result = RemoveCookie(productCode, cart);
                    if(string.IsNullOrEmpty(result.Values.FirstOrDefault()))
                    {
                        HttpContext.Response.Cookies.Delete("MyCart");
                        Basket emptyBasket = new("0");
                        return PartialView("_PartialCart", emptyBasket);
                    }
                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault());

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                        return BadRequest(new { message });
                    }

                    Dictionary<string, int> products = GetProductsFromCookie(result.Values.FirstOrDefault());
                    Basket cookieBasket = new("0");
                    foreach (var product in products)
                    {
                        decimal price = await _productService.GetPriceAsync(product.Key);
                        string productName = await _productService.GetProductNameAsync(product.Key);
                        string imagePath = await _productService.GetImagePathAsync(product.Key);
                        cookieBasket.AddItem(product.Key, price, product.Value, "0", productName,imagePath);
                        foreach (var item in cookieBasket.Items)
                        {
                            if (item.ProductCode == product.Key)
                            {
                                item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == product.Key).FirstOrDefaultAsync();
                                break;
                            }
                        }
                    }
                    return PartialView("_PartialCart",cookieBasket); 

                }
                //db
                bool resultDb = await _basketService.RemoveBasketItemAsync(userID,productCode);
                if(resultDb)
                    message = "Ürün Sepetten Çıkarıldı";
                else
                    message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                totalProductCount = await _basketService.CountTotalBasketItems(userID);
                TempData["message"] = message;
                Basket basket = await _basketService.GetBasketAsync(userID);
                return PartialView("_PartialCart",basket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                string message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                return BadRequest(new { message });
            }
        }

        //-------------shop ends -------------//



        //-------------checkout-------------//
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
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                    user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                }
                else
                   return View("NotFound");

                bool isInBuyRegion = false;
                
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                if(ip == "::1")
                    ip = "212.252.136.146";
                //isInBuyRegion = await _userAuditService.IsInBuyRegion(userID,ip);
                
                //if(!isInBuyRegion)
                //{
                //    TempData["WarningMessage"] = "Hizmetimiz Türkiye sınırları içinde geçerlidir.";
                //    return RedirectToAction("Index", "Home");
                //}
                var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
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
                    firstName = GetFirstName(fullName);
                    lastName = GetLastName(fullName);
                    shipToAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, true, 0, string.Empty, firstName, lastName, email, phone, false, string.Empty);
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
                    firstName = GetFirstName(fullName);
                    lastName = GetLastName(fullName);
                    billingAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, 0, string.Empty, firstName, lastName, email, phone, false, string.Empty);

                }

                Order order = new(userID, shipToAddress, billingAddress, orderItems, isInBuyRegion, true, 1);
                if(order.TotalAmount>100000)
                {
                    TempData["DangerMessage"] = "Sepet Miktarı 100000₺'den Büyük Olamaz.";
                    return RedirectToAction("Cart");
                }

                return View(order);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
               return View("NotFound");
            }
        }
        public IActionResult _PartialIsCorporate(Models.OrderAggregate.Address address)
        {
            try
            {
                if (ModelState.IsValid)
                    return PartialView(address);

                Models.OrderAggregate.Address emptyAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
                return PartialView(address);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                Models.OrderAggregate.Address emptyAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
                return PartialView(emptyAddress);
            }
        }
        [HttpPost]
        public IActionResult _PartialIsCorporate(string firstName, string lastName, string corporateName, int vATNumber, string vATState, bool isCorporate)
        {
            try
            {

                if (string.IsNullOrEmpty(firstName))
                    firstName = string.Empty;
                if (string.IsNullOrEmpty(lastName))
                    lastName = string.Empty;
                if (string.IsNullOrEmpty(corporateName))
                    corporateName = string.Empty;
                if (string.IsNullOrEmpty(vATState))
                    vATState = string.Empty;


                Models.OrderAggregate.Address address = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, vATNumber, vATState, firstName, lastName, string.Empty, string.Empty, isCorporate, corporateName);
                return PartialView(address);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                Models.OrderAggregate.Address emptyAddress = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, true, true, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
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
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                string? buyerID = _userManager.GetUserId(User);

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
                var _orderService = _serviceProvider.GetRequiredService<IOrderService>();
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

                return Json(new { success = true, redirectUrl = Url.Action("Payment") });
   
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return Ok(new { success = false, message = "Kargo ve Fatura Bilgileri Kaydedilirken Hata ile Karşılaşıldı. " });
            }
        }
        [HttpGet]
        public IActionResult Payment()
        {
            try
            {
                var paymentJson = HttpContext.Session.GetString("PaymentViewModel");

                if (string.IsNullOrEmpty(paymentJson))
                    return RedirectToAction("Not Found"); // Or show a nice message

                var paymentModel = JsonConvert.DeserializeObject<PaymentViewModel>(paymentJson);

                return View("Payment", paymentModel);
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
                var _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                string? buyerID = _userManager.GetUserId(User);

                var _userAuditService = _serviceProvider.GetRequiredService<IUserAuditService>();
                UserAudit userAudit = await _userAuditService.GetUsurAuditAsync(buyerID);
                var _orderService = _serviceProvider.GetRequiredService<IOrderService>();
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
                if(!DateTime.TryParse(userAudit.RegistrationDate.ToString(), out registrationDate))
                    return Ok(new { success = false, message = "Kayıt Tarihi Bilgileri Bulunamadı." });

                model.RegistrationDate = lastLoginDate;
                model.LastLoginDate = registrationDate;
                model.Ip = "127.0.0.1";
                model.City = "İzmir";
                model.Country = "Türkiye";
                string resultString = string.Empty;
                if(model.PaymentType == 1)
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
                        var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
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
                var iyzipayService = _serviceProvider.GetRequiredService<IIyzipayPaymentService>();
                var _orderService = _serviceProvider.GetRequiredService<IOrderService>();
                ThreedsPayment payment = await iyzipayService.Payment3dsCallBack(Request.Form["conversationId"], Request.Form["paymentId"]);


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

                    var _basketService = _serviceProvider.GetRequiredService<IBasketService>();
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

                var _orderService = _serviceProvider.GetRequiredService<IOrderService>();
                InstallmentDetail installmentInfo = await _orderService.GetInstallmentInfoAsync(model.BinNumber, model.Price);
                if(installmentInfo == null)
                    return StatusCode(500, new { success = false, message = "Hata ile Karşılaşıldı. Lütfen Tekrar Deneyiniz." });

                return Ok(new { installments = installmentInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return StatusCode(500, new { success = false, message = "Hata ile Karşılaşıldı. Lütfen Tekrar Deneyiniz." });
            }
        }

        //-------------checkout ends -------------//


        //-------------mail-------------//
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Subscribe(string emailAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(emailAddress))
                    return Ok(new { success = false, message = "Email adresi boş olamaz." });

                // Validate email format
                if (!IsValidEmail(emailAddress))
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
        public async Task<IActionResult> SendEmail(string username,string emailAddress,string phone,string message,string subject)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                    return Ok(new { success = false, message = "İsim boş olamaz." });
                if (string.IsNullOrEmpty(emailAddress))
                    return Ok(new { success = false, message = "Email adresi boş olamaz." });
                // Validate email format
                if (!IsValidEmail(emailAddress))
                    return Ok(new { success = false, message = "Hatalı Email formatı." });
                if (string.IsNullOrEmpty(message))
                    return Ok(new { success = false, message = "Mesaj boş olamaz." });
                if(string.IsNullOrEmpty(subject))
                    return Ok(new { success = false, message = "Konu boş olamaz." });
                var _emailSender = _serviceProvider.GetRequiredService<IEmailSender>();
                string result = await _emailSender.SendContactUsEmailAsync(username,emailAddress,phone,message,subject);
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
