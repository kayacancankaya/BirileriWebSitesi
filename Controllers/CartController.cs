using BirileriWebSitesi.Data;
using BirileriWebSitesi.Helpers;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.InquiryAggregate;
using BirileriWebSitesi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BirileriWebSitesi.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IProductService _productService;
        private readonly IBasketService _basketService;
        public CartController(ILogger<CartController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager,  
                                IProductService productService,IBasketService basketService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _productService = productService;
            _basketService = basketService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                string? userID = string.Empty;
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
                    Dictionary<string, int> productInfo = CookieHelper.GetProductsFromCookie(cart);
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
                        await _basketService.AddItemToAnonymousBasketAsync(anonymousBasket, productCode, unitPrice, quantity);

                    }

                    return View(anonymousBasket);
                }

                Basket basket = await _basketService.GetBasketAsync(userID);
                return View(basket);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return RedirectToAction("NotFound","Home");
            }
        }
        public async Task<IActionResult> Inquiry()
        {
            try
            {
                string? userID = string.Empty;
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }
            
                if (string.IsNullOrEmpty(userID))
                {
                    string? cart = Request.Cookies["MyInquiry"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        Inquiry emptyBasket = new("0");
                        return View(emptyBasket);
                    }
                    Dictionary<string, int> productInfo = CookieHelper.GetProductsFromCookie(cart);
                    Inquiry anonymousBasket = new("0");
                    string productCode = string.Empty;
                    int quantity = 0;
                    decimal unitPrice = decimal.Zero;
                    decimal totalAmount = 0;
                    foreach (var product in productInfo)
                    {
                        productCode = product.Key;
                        quantity = product.Value;
                        unitPrice = await _productService.GetPriceAsync(productCode);
                        await _basketService.AddItemToAnonymousInquiryBasketAsync(anonymousBasket, productCode, unitPrice, quantity);

                    }

                    return View(anonymousBasket);
                }

                Inquiry basket = await _basketService.GetInquiryBasketAsync(userID);
                return View(basket);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return RedirectToAction("NotFound","Home");
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddToCart(string userId, string productCode, decimal price, int quantity)
        {
            try
            {
                if (price * quantity > 100000)
                    return Ok(new { success = false, message = "Sepet Miktarı 100.000₺'den Büyük Olamaz." });
                

                string totalProduct = string.Empty;
                Dictionary<int, string> result = new();
                if (string.IsNullOrEmpty(productCode) ||
                    price <= 0 ||
                    quantity <= 0)
                    return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });

                //check if it is not a variant if so get its variant
                var isExists = await _context.Products.Where(p => p.ProductCode == productCode).AnyAsync();
                if(isExists)
                {
                    productCode = await _context.ProductVariants.Where(p => p.BaseProduct == productCode)
                                                           .OrderBy(p => p.ProductCode)
                                                           .Select(p => p.ProductCode)
                                                           .FirstOrDefaultAsync();
                }
                    //cookie 
                if (userId == "0")
                {
                    string cart = Request.Cookies["MyCart"];
                    var cookieOptions = new CookieOptions();
                    cookieOptions = new CookieOptions();
                    cookieOptions.Expires = DateTime.Now.AddDays(30);
                    cookieOptions.Path = "/";

                    result = CookieHelper.AddBasketCookie(cart, productCode, quantity);

                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault(), cookieOptions);
                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        TempData["TotalProduct"] = 0;
                        return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });
                    }
                    totalProduct = result.Keys.FirstOrDefault().ToString();
                    return Ok(new { success = true, message = "Ürün Sepete Eklendi", totalProduct });
                }

                //db
                result = await _basketService.AddItemToBasketAsync(userId, productCode, price, quantity);
                if (result.Values.FirstOrDefault() == "Ürün Sepete Eklenirken Hata ile Karşılaşıldı")
                {
                    return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });
                }
                totalProduct = result.Keys.FirstOrDefault().ToString();

                return Ok(new { success = true, message = "Ürün Sepete Eklendi", TotalProduct = totalProduct });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message.ToString());
                return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });

            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddToInquiry(string userId, string productCode, decimal price, int quantity)
        {
            try
            {
              
                string totalProduct = string.Empty;
                Dictionary<int, string> result = new();
                if (string.IsNullOrEmpty(productCode) ||
                    price <= 0 ||
                    quantity <= 0)
                    return Ok(new { success = false, message = "Ürün Sepete Eklenirken Hata ile Karşılaşıldı." });

                //check if it is not a variant if so get its variant
                var isExists = await _context.Products.Where(p => p.ProductCode == productCode).AnyAsync();
                if(isExists)
                {
                    productCode = await _context.ProductVariants.Where(p => p.BaseProduct == productCode)
                                                           .OrderBy(p => p.ProductCode)
                                                           .Select(p => p.ProductCode)
                                                           .FirstOrDefaultAsync();
                }
                    //cookie 
                if (userId == "0")
                {
                    string cart = Request.Cookies["MyInquiry"];
                    var cookieOptions = new CookieOptions();
                    cookieOptions = new CookieOptions();
                    cookieOptions.Expires = DateTime.Now.AddDays(30);
                    cookieOptions.Path = "/";

                    result = CookieHelper.AddBasketCookie(cart, productCode, quantity);

                    HttpContext.Response.Cookies.Append("MyInquiry", result.Values.FirstOrDefault(), cookieOptions);
                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        TempData["TotalProduct"] = 0;
                        return Ok(new { success = false, message = "Ürün İstek Sepetine Eklenirken Hata ile Karşılaşıldı." });
                    }
                    totalProduct = result.Keys.FirstOrDefault().ToString();
                    return Ok(new { success = true, message = "Ürün İstek Sepetine Eklendi", totalProduct });
                }

                //db
                result = await _basketService.AddItemToInquiryBasketAsync(userId, productCode, price, quantity);
                if (result.Values.FirstOrDefault() == "Ürün İstek Sepetine Eklenirken Hata ile Karşılaşıldı")
                {
                    return Ok(new { success = false, message = "Ürün İstek Sepetine Eklenirken Hata ile Karşılaşıldı." });
                }
                totalProduct = result.Keys.FirstOrDefault().ToString();

                return Ok(new { success = true, message = "Ürün İstek Sepetine Eklendi", TotalProduct = totalProduct });
            }
            catch
            {
                return Ok(new { success = false, message = "Ürün İstek Sepetine Eklenirken Hata ile Karşılaşıldı." });

            }
        }
        [HttpPost]
        public async Task<IActionResult> InitializeCartNumber()
        {
            try
            {
                string? userID = string.Empty;

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
                        message = "0";
                        return Ok(new { message });

                    }

                    string[] cartArray = cart.Split('&');
                    message = cartArray.Count().ToString();
                    return Ok(new { message });
                }

                //db
                int result = 0;
                result = await _basketService.CountDistinctBasketItems(userID);
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
        [HttpPost]
        public async Task<IActionResult> InitializeInquiryNumber()
        {
            try
            {
                string? userID = string.Empty;

                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyInquiry"];
                    if (string.IsNullOrEmpty(cart))
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
                result = await _basketService.CountDistinctInquiryBasketItems(userID);
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
        public async Task<IActionResult> CartItemAmountChanged(string productCode, int quantity)
        {
            try
            {
                if (quantity < 0)
                    quantity = 0;
                string? userID = string.Empty;
                Dictionary<int, string> result = new();
                int totalProductCount = 0;

                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                Basket basket;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyCart"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        message = "Sepet Bulunamadı";
                        return BadRequest(new { message });
                    }

                    result = CookieHelper.UpdateCookie(productCode, quantity, cart);
                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault());

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                        return BadRequest(new { message });
                    }
                    TempData["message"] = message;
                    TempData["message"] = totalProductCount;
                    Dictionary<string, int> products = CookieHelper.GetProductsFromCookie(result.Values.FirstOrDefault());
                    basket = new("0");
                    foreach (var product in products)
                    {
                        decimal price = await _productService.GetPriceAsync(product.Key);
                        await _basketService.AddItemToAnonymousBasketAsync(basket, product.Key, price, product.Value);
                    }

                }
                else
                {
                    //db
                    basket = await _basketService.SetQuantity(userID, productCode, quantity);
                    if (basket == null)
                        basket = new(userID);
                }
                // Get updated totals
                var item = basket.Items.FirstOrDefault(x => x.ProductCode == productCode);
                decimal itemTotal = item != null ? item.Quantity * item.UnitPrice : 0;
                decimal cartTotal = basket.Items.Sum(x => x.Quantity * x.UnitPrice);
                return Json(new
                {
                    success = true,
                    productCode = productCode,
                    itemTotal = itemTotal.ToString("C"),
                    cartTotal = cartTotal.ToString("C")
                });
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
        public async Task<IActionResult> InquiryItemAmountChanged(string productCode, int quantity)
        {
            try
            {
                string? userID = string.Empty;
                Dictionary<int, string> result = new();
                int totalProductCount = 0;

                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyInquiry"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        message = "İstek Sepet iBulunamadı";
                        return BadRequest(new { message });
                    }

                    result = CookieHelper.UpdateCookie(productCode, quantity, cart);
                    HttpContext.Response.Cookies.Append("MyInquiry", result.Values.FirstOrDefault());

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        message = "Ürün İstek Sepetinden Çıkarılırken Hata ile Karşılaşıldı";
                        return BadRequest(new { message });
                    }
                    TempData["message"] = message;
                    TempData["message"] = totalProductCount;
                    Dictionary<string, int> products = CookieHelper.GetProductsFromCookie(result.Values.FirstOrDefault());
                    Inquiry cookieBasket = new("0");
                    foreach (var product in products)
                    {
                        decimal price = await _productService.GetPriceAsync(product.Key);
                        await _basketService.AddItemToAnonymousInquiryBasketAsync(cookieBasket, product.Key, price, product.Value);
                    }
                    return PartialView("_PartialInquiry", cookieBasket);

                }
                //db
                Inquiry basket = await _basketService.SetInquiryQuantity(userID, productCode, quantity);
                if (basket == null)
                    basket = new(userID);

                return PartialView("_PartialInquiry", basket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                string message = "Ürün İstek Sepetinden Çıkarılırken Hata ile Karşılaşıldı";
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

                    result = CookieHelper.RemoveCookie(productCode, cart);
                    if (string.IsNullOrEmpty(result.Values.FirstOrDefault()))
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

                    Dictionary<string, int> products = CookieHelper.GetProductsFromCookie(result.Values.FirstOrDefault());
                    Basket cookieBasket = new("0");
                    foreach (var product in products)
                    {
                        decimal price = await _productService.GetPriceAsync(product.Key);
                        await _basketService.AddItemToAnonymousBasketAsync(cookieBasket, product.Key, price, product.Value);
                        foreach (var item in cookieBasket.Items)
                        {
                            if (item.ProductCode == product.Key)
                            {
                                item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == product.Key).FirstOrDefaultAsync();
                                break;
                            }
                        }
                    }
                    
                    return PartialView("_PartialCart", cookieBasket);

                }
                //db
                bool resultDb = await _basketService.RemoveBasketItemAsync(userID, productCode);
                if (resultDb)
                    message = "Ürün Sepetten Çıkarıldı";
                else
                    message = "Ürün Sepetten Çıkarılırken Hata ile Karşılaşıldı";
                totalProductCount = await _basketService.CountTotalBasketItems(userID);
                TempData["message"] = message;
                Basket basket = await _basketService.GetBasketAsync(userID);
                return PartialView("_PartialCart", basket);
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
        public async Task<IActionResult> RemoveInquiryItem(string productCode)
        {
            try
            {
                string? userID = string.Empty;
                Dictionary<int, string> result = new();
                int totalProductCount = 0;
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                }

                string message = string.Empty;
                //cookie 
                if (string.IsNullOrEmpty(userID))
                {
                    string cart = Request.Cookies["MyInquiry"];
                    if (string.IsNullOrEmpty(cart))
                    {
                        message = "İstek Sepeti Bulunamadı";
                        return BadRequest(new { message });
                    }

                    result = CookieHelper.RemoveCookie(productCode, cart);
                    if (string.IsNullOrEmpty(result.Values.FirstOrDefault()))
                    {
                        HttpContext.Response.Cookies.Delete("MyInquiry");
                        Inquiry emptyBasket = new("0");
                        return PartialView("_PartialInquiry", emptyBasket);
                    }
                    HttpContext.Response.Cookies.Append("MyInquiry", result.Values.FirstOrDefault());

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        message = "Ürün İstek Sepetinden Çıkarılırken Hata ile Karşılaşıldı";
                        return BadRequest(new { message });
                    }

                    Dictionary<string, int> products = CookieHelper.GetProductsFromCookie(result.Values.FirstOrDefault());
                    Inquiry cookieBasket = new("0");
                    foreach (var product in products)
                    {
                        decimal price = await _productService.GetPriceAsync(product.Key);
                        await _basketService.AddItemToAnonymousInquiryBasketAsync(cookieBasket, product.Key, price, product.Value);
                        foreach (var item in cookieBasket.Items)
                        {
                            if (item.ProductCode == product.Key)
                            {
                                item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == product.Key).FirstOrDefaultAsync();
                                break;
                            }
                        }
                    }

                    return PartialView("_PartialInquiry", cookieBasket);

                }
                //db
                bool resultDb = await _basketService.RemoveInquiryBasketItemAsync(userID, productCode);
                if (resultDb)
                    message = "Ürün İstek Sepetinden Çıkarıldı";
                else
                    message = "Ürün İstek Sepetinden Çıkarılırken Hata ile Karşılaşıldı";
                totalProductCount = await _basketService.CountTotalInquiryBasketItems(userID);
                TempData["message"] = message;
                Inquiry basket = await _basketService.GetInquiryBasketAsync(userID);
                return PartialView("_PartialInquiry", basket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                string message = "Ürün İstek Sepetinden Çıkarılırken Hata ile Karşılaşıldı";
                return BadRequest(new { message });
            }
        }
       
    }
}
