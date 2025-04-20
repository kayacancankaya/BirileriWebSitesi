using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.OrderAggregate;
using BirileriWebSitesi.Models.ViewModels;
using BirileriWebSitesi.Services;
using Iyzipay.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
namespace BirileriWebSitesi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBasketService _basketService;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IUserAuditService _userAuditService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private string? authCookie = string.Empty;
        string MyCart = string.Empty;
        public HomeController(ILogger<HomeController> logger,
                            ApplicationDbContext context,
                            IBasketService basketService,
                            IProductService productService,
                            IOrderService orderService,
                            IUserService userService,
                            IUserAuditService userAuditService,
                            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _basketService = basketService;
            _orderService = orderService;
            _productService = productService;
            _userService = userService;
            _userManager = userManager;
            _userAuditService = userAuditService;
        }

        public IActionResult Index()
        {
            authCookie = Request.Cookies[".AspNetCore.Identity.Application"];
            if (authCookie != null)
                TempData["Cookie"] = "exists";
            else
                TempData["Cookie"] = "not exists";
			return View();
        }
        //-------------shop-------------//
        public IActionResult Shop()
        {
            try
            {

                //get all categories
                IEnumerable<Catalog> catalogs = _context.Catalogs.ToList();
                //initiate view model
                PaginationViewModel pagination = new PaginationViewModel();
                IEnumerable<Product> products = new List<Product>();
                // get products
                products = _context.Products.Where(a => a.IsActive == true)
                                            .Take(PaginationViewModel.PageSize)
                                            .Include(p => p.ProductVariants)
                                             .OrderByDescending(p=>p.Popularity)
                                             .ToList();
                //get total products
                pagination.TotalCount = _context.Products.Count();
                if(products == null)
                    return NotFound();

                //get popular products
                IEnumerable<Product> popularProducts = _context.Products.OrderByDescending(p => p.Popularity)
                                                                        .Where(a=>a.IsActive == true)
                                                                         .Take(3);

                //assign values to vievmodel
                pagination.CurrentPage = 1;
                pagination.TotalPage = (int)Math.Ceiling((double)pagination.TotalCount / PaginationViewModel.PageSize);

                ShopViewModel shop = new ShopViewModel();
                ProductCardViewModel productCard = new ProductCardViewModel();
                productCard.products = products;
                productCard.pagination = pagination;
                shop.productCard = productCard;
                shop.Catalogs = catalogs;
                shop.PopularProducts = popularProducts;
                
                return View(shop);

            }
            catch (Exception)
            {
                return NotFound();
            }
        }
        public async Task<IActionResult> Cart()
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
            catch (Exception)
            {
                return NotFound();
            }
        }
        public IActionResult ShopFiltered(int catalogID,string searchFilter,int pageNumber,decimal minPrice, decimal maxPrice)
        {
            try
            {
                
                IEnumerable<Product> products = new List<Product>();
                int totalCount = 0;
                int totalPage = 0;
                if (searchFilter == null)
                    searchFilter = string.Empty;
                // get products
                products = _context.Products
                                     .Where(n => n.ProductName.ToLower().Contains(searchFilter.ToLower()) &&
                                                 n.BasePrice >= minPrice && n.BasePrice <= maxPrice &&
                                                 (catalogID == 0 || n.CatalogId == catalogID) &&
                                                 n.IsActive == true)
                                     .Skip((pageNumber - 1) * PaginationViewModel.PageSize)
                                     .Take(PaginationViewModel.PageSize)
                                     .Include(d => d.Discounts)
                                     .Include(p => p.ProductVariants)
                                     .ToList();

                //filter related discounts

                foreach (Product product in products)
                {
                    product.Discounts = product.Discounts.OrderByDescending(d => d.StartDate)
                                                            .Where(d => d.StartDate <= DateTime.Now &&
                                                                        d.EndDate >= DateTime.Now)
                                                            .ToList();
                    if(product.Discounts== null)
                        product.Discounts = new List<Discount>();
                }
                //get total products
                totalCount = _context.Products
                                     .Where(n => n.ProductName.ToLower().Contains(searchFilter.ToLower()) &&
                                                 n.BasePrice >= minPrice && n.BasePrice <= maxPrice &&
                                                 (catalogID == 0 || n.CatalogId == catalogID) &&
                                                 n.IsActive == true)
                                     .Count();

                totalPage = (int)Math.Ceiling((double)totalCount / PaginationViewModel.PageSize);

                ProductCardViewModel model = new();
                model.products = products;
                PaginationViewModel pagination = new();
                pagination.TotalCount = totalCount;
                pagination.TotalPage = totalPage;
                pagination.CurrentPage = pageNumber;
                model.pagination = pagination;


                return PartialView("_PartialProductCard", model);

            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Filtreleme iþlemi esnasýnda hata ile karþýlaþýldý.Lütfen daha sonra tekrar deneyiniz." });
            }
        }
        public IActionResult ProductDetailed(string productCode)
        {
            try
            {
                //get product
                Product? product = _context.Products.Where(c=>c.ProductCode == productCode)
                                                    .Include(v=>v.ProductVariants)
                                                    .FirstOrDefault();

                if(product == null) 
                    return NotFound();
                
                string productVariant = product.ProductVariants.First().ProductCode;//to fetch global variants
                Dictionary<string,string> globalVariants = new Dictionary<string,string>();
                string? variantKey = string.Empty;
                string? variantValue = string.Empty;
                Dictionary<string, string> variantAttributes = new Dictionary<string, string>();
                string? variantAttribute = string.Empty;
                string? variantAttributeValue = string.Empty;
                int counter = 0;
                string initialVariantName = string.Empty;
                for (int i = 11; i < productVariant.Length; i += 6)
                {
                    if(productVariant.Substring(productVariant.Length-1,1) == "B" && 
                        i+1 == productVariant.Length)
                        break;

                    //fetch global variants
                    variantKey = productVariant.Substring(i, 3);
                    variantValue = _context.Variants.Where(v => v.VariantCode == variantKey).Select(n => n.VariantName).FirstOrDefault();
                    if (!string.IsNullOrEmpty(variantValue) &&
                        !globalVariants.ContainsKey(variantKey))
                        globalVariants.Add(variantKey, variantValue);
                    //fetch variant attributes of global variant 
                    counter = 0;
                    foreach (var variant in product.ProductVariants)
                    {
                        variantAttribute = variant.ProductCode.Substring(i + 3, 3);
                        variantAttributeValue = _context.VariantAttributes.Where(v => v.VariantCode == variantKey &&
                                                                                    v.VariantAttributeCode == variantAttribute)
                                                                            .Select(n => n.VariantAttributeName).FirstOrDefault();
                        //get first names of first variants to display initializing page
                        if(counter == 0)
                        {
                            initialVariantName = string.Format("{0} {1}", initialVariantName, variantAttributeValue);
                            counter++;
                        }
                        if (!string.IsNullOrEmpty(variantAttributeValue) &&
                            !variantAttributes.ContainsKey(variantKey+variantAttribute))
                            variantAttributes.Add(variantKey + variantAttribute, variantAttributeValue);
                    }
                }
                //get variant info to display initializing page
                    ProductDetailedVariantInfoViewModel initialVariantInfo = new();
                initialVariantInfo.VariantName = product.ProductName + " " + initialVariantName;
                initialVariantInfo.VariantPrice = product.ProductVariants.FirstOrDefault().Price;
                //get image path of variant to display initializing page
                ProductDetailedVariantImageViewModel initialVariantImage = new();
                initialVariantImage.FilePath = string.Format("~/images/resource/products/{0}/1.jpg", product.ProductVariants.First().ImagePath);
                initialVariantImage.ProductVariantName = string.Format("{0},{1}", product.ProductName, initialVariantName);

                //get related products
                List<string> relatedProductCodes = _context.RelatedProducts.Where(c=>c.ProductCode == productCode).Select(r=>r.RelatedProductCode).ToList();
                List<Product> relatedProducts = new List<Product>();

                foreach (var relatedProductCode in relatedProductCodes)
                {
                    Product? relatedProduct = _context.Products.Where(c => c.ProductCode == relatedProductCode).FirstOrDefault();
                    if (relatedProduct != null)
                        relatedProducts.Add(relatedProduct);
                }
                IEnumerable<Product> popularProducts = _context.Products.OrderByDescending(p => p.Popularity)
                                                                        .Where(a => a.IsActive == true)
                                                                         .Take(3);

                ProductDetailedViewModel model = new();
                model.Product = product;
                model.GlobalVariants = globalVariants;
                model.VariantAttributes = variantAttributes;
                model.RelatedProducts = relatedProducts;
                model.PopularProducts = popularProducts;
                model.ProductVariantInfo = initialVariantInfo;
                model.ProductVariantImage = initialVariantImage;
                return View(model);

            }
            catch (Exception ex)
            {
                string err = ex.Message.ToString();
                return NotFound();
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddToCart(string userId, string productCode, decimal price, int quantity)
        {
            try
            {

                string totalProduct = string.Empty;
                Dictionary<int, string> result = new();
                if (string.IsNullOrEmpty(productCode) ||
                    price <= 0 ||
                    quantity <= 0)
                    return BadRequest("Ürün Sepete Eklenirken Hata Ýle Karþýlaþýldý.");

                //cookie 
                if (userId == "0")
                {
                    result = AddBasketCookie(productCode, quantity);
                    if(result.Values.FirstOrDefault() == "HATA")
                    {
                        TempData["TotalProduct"] = 0;
                        return BadRequest("Ürün Sepete Eklenirken Hata Ýle Karþýlaþýldý.");
                    }
                    totalProduct = result.Keys.FirstOrDefault().ToString();
                    return Ok(new { message = "Ürün Sepete Eklendi", totalProduct });
                }

                //db
                result =  await _basketService.AddItemToBasketAsync(userId, productCode, price, quantity);
                if (result.Values.FirstOrDefault() == "Ürün Sepete Eklenirken Hata Ýle Karþýlaþýldý")
                {
                    return BadRequest("Ürün Sepete Eklenirken Hata Ýle Karþýlaþýldý.");
                }
                totalProduct = result.Keys.FirstOrDefault().ToString();
                return Ok(new { message = "Ürün Sepete Eklendi", TotalProduct = totalProduct });
            }
            catch
            {
                return BadRequest("Ürün Sepete Eklenirken Hata Ýle Karþýlaþýldý.");
            }
        }
        [HttpPost]
        public async Task<IActionResult> InitializeCartNumber()
        {
            try
            {
                string? userID = string.Empty;
                if(User.Identity.IsAuthenticated)
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
                result =  await _basketService.CountDistinctBasketItems(userID);
                message = result.ToString();
                return Ok(new { message });
            }
            catch
            {
                string message = "0";
                return BadRequest(new { message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CartItemAmountChanged(string productCode,int quantity)
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
                        message = "Sepet Bulunamadý";
                        return BadRequest(new { message });
                    }

                    result = UpdateCookie(productCode, quantity, cart);
                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault());

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        message = "Ürün Sepetten Çýkarýlýrken Hata Ýle Karþýlaþýldý";
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
            catch
            {
                string message = "Ürün Sepetten Çýkarýlýrken Hata Ýle Karþýlaþýldý";
                return BadRequest(new { message });
            }
        }
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
                        message = "Sepet Bulunamadý";
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
                        message = "Ürün Sepetten Çýkarýlýrken Hata Ýle Karþýlaþýldý";
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
                    message = "Ürün Sepetten Çýkarýldý";
                else
                    message = "Ürün Sepetten Çýkarýlýrken Hata Ýle Karþýlaþýldý";
                totalProductCount = await _basketService.CountTotalBasketItems(userID);
                TempData["message"] = message;
                Basket basket = await _basketService.GetBasketAsync(userID);
                return PartialView("_PartialCart",basket);
            }
            catch
            {
                string message = "Ürün Sepetten Çýkarýlýrken Hata Ýle Karþýlaþýldý";
                return BadRequest(new { message });
            }
        }
        public IActionResult _PartialProductCard(IEnumerable<Product> products)
        {
            try
            {

                if (products == null)
                    return Ok(new { success = false, message = "Ürün Bulunamadý." });
                if (!products.Any())
                    return Ok(new { success = false, message = "Ürün Bulunamadý." });


                return PartialView("_PartialProductCard", products);

            }
            catch (Exception)
            {
                return Ok(new { success = false, message = "Ürün Listelenirken Hata Ýle Karþýlaþýldý." });
            }
        }
        public async Task<IActionResult> GetPartialViewsForProductVariant(string productCode,string productName, Dictionary<string,string> variantAttributes)
        {
            try
            {
                if (string.IsNullOrEmpty(productCode) ||
                    string.IsNullOrEmpty(productName) ||
                    variantAttributes == null)
                {
                    return BadRequest(new { success = false, message = "Varyant Seçeneði Bulunamadý." });
                }
                string variantCode = productCode;
                string variantName = productName;
                foreach (var item in variantAttributes)
                {
                    variantCode = variantCode + item.Key;
                    variantName = variantName + " " + item.Value;
                }

                string filePath = string.Format("~/images/resource/products/{0}/1.jpg",
                                                await _context.ProductVariants.Where(v => v.ProductCode == variantCode).Select(f => f.ImagePath).FirstOrDefaultAsync());
                
                ProductDetailedVariantImageViewModel imageModel = new();
                imageModel.FilePath = filePath;
                imageModel.ProductVariantName = variantName;
                decimal variantPrice = await _context.ProductVariants.Where(v => v.ProductCode == variantCode)
                                                                     .Select(p => p.Price)
                                                                     .FirstOrDefaultAsync();

                ProductDetailedVariantInfoViewModel infoModel = new();
                infoModel.VariantCode = variantCode;
                infoModel.VariantName = variantName;
                infoModel.VariantPrice = variantPrice;
                

                var imagePartial = RenderPartialViewToString("_PartialProductVariantImage", imageModel);
                var infoPartial = RenderPartialViewToString("_PartialProductVariantInfo", infoModel);

                // Return JSON response with both partial views
                return Json(new { imagePartial, infoPartial });

            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Beklenmeyen Bir Hata Oluþtu"});
            }
        }
        public async Task<IActionResult> _CatalogPartial()
        {
            try
            {
                IEnumerable<Catalog> catalogs = await _context.Catalogs.ToListAsync();
                if (catalogs == null)
                    return BadRequest();
                if (!catalogs.Any())
                    return BadRequest();
                return PartialView("_CatalogPartial", catalogs);
            }
            catch (Exception ex)
            {
                return BadRequest();
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
                if (User.Identity.IsAuthenticated)
                {
                    userID = _userManager.GetUserId(User);
                    user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                }
                else
                    return NotFound();

                bool isInBuyRegion = false;
                
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                if(ip == "::1")
                    ip = "212.252.136.146";
                isInBuyRegion = await _userAuditService.IsInBuyRegion(userID,ip);
                
                if(!isInBuyRegion)
                {
                    TempData["WarningMessage"] = "Hizmetimiz Türkiye sınırları içinde geçerlidir.";
                    return RedirectToAction("Index", "Home");
                }

                Basket basket = await _basketService.GetBasketAsync(userID);
                List<Models.OrderAggregate.OrderItem> orderItems = new();
                foreach (Models.BasketAggregate.BasketItem item in basket.Items)
                {
                    Models.OrderAggregate.OrderItem orderItem = new(item.ProductCode, item.Quantity, item.UnitPrice, item.ProductName);

                    orderItems.Add(orderItem);
                }

                Models.OrderAggregate.Address? shipToAddress = await _context.Addresses.OrderByDescending(i => i.Id)
                                                                .Where(i => i.UserId == userID &&
                                                                          i.IsBilling == false &&
                                                                          i.SetAsDefault == true)
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

                Models.OrderAggregate.Address? billingAddress = await _context.Addresses.OrderByDescending(i => i.Id)
                                                                .Where(i => i.UserId == userID &&
                                                                          i.IsBilling == true &&
                                                                          i.SetAsDefault == true)
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
                return View(order);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return NotFound();
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
                await _orderService.SaveOrderInfoAsync(order);
                int orderID = await _orderService.GetOrderID(order);
                if(orderID == 0)
                    return StatusCode(500, new { success = false, message = "Kargo ve Fatura Bilgileri Kaydedilirken Hata ile Karşılaşıldı. " });
               
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
                return StatusCode(500, new { success = false, message = "Kargo ve Fatura Bilgileri Kaydedilirken Hata ile Karşılaşıldı. " });
            }
        }
        [HttpGet]
        public IActionResult Payment()
        {
            var paymentJson = HttpContext.Session.GetString("PaymentViewModel");

            if (string.IsNullOrEmpty(paymentJson))
                return RedirectToAction("Not Found"); // Or show a nice message

            var paymentModel = JsonConvert.DeserializeObject<PaymentViewModel>(paymentJson);

            return View("Payment", paymentModel);
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

                    return BadRequest(new { success = false, message = "Sipariş Kaydedilirken Hata ile Karşılaşıldı.", errors });
                }

                string? buyerID = _userManager.GetUserId(User);

            
                UserAudit userAudit = await _userAuditService.GetUsurAuditAsync(buyerID);
                if(userAudit == null)
                    return BadRequest(new { success = false, message = "Kullanıcı Bilgileri Bulunamadı." });
                if (string.IsNullOrEmpty(userAudit.Ip))
                    return BadRequest(new { success = false, message = "IP Bilgileri Bulunamadı." });
                if (string.IsNullOrEmpty(userAudit.City))
                    return BadRequest(new { success = false, message = "Şehir Bilgileri Bulunamadı." });
                if (string.IsNullOrEmpty(userAudit.Country))
                    return BadRequest(new { success = false, message = "Ülke Bilgileri Bulunamadı." });
                if (userAudit.RegistrationDate == null)
                    return BadRequest(new { success = false, message = "Kayıt Tarihi Bilgileri Bulunamadı." });
                if (userAudit.LastLoginDate == null)
                    return BadRequest(new { success = false, message = "Son Giriş Tarihi Bilgileri Bulunamadı." });
                DateTime lastLoginDate;
                DateTime registrationDate;
                if (!DateTime.TryParse(userAudit.LastLoginDate.ToString(), out lastLoginDate))
                    return BadRequest(new { success = false, message = "Son Giriş Tarihi Bilgileri Bulunamadı." });
                if(!DateTime.TryParse(userAudit.RegistrationDate.ToString(), out registrationDate))
                    return BadRequest(new { success = false, message = "Kayıt Tarihi Bilgileri Bulunamadı." });

                model.RegistrationDate = lastLoginDate;
                model.LastLoginDate = registrationDate;
                model.Ip = userAudit.Ip;
                model.City = userAudit.City;
                model.Country = userAudit.Country;
                string resultString = string.Empty;
                if(model.PaymentType == 1)
                {
                    if (!model.Force3Ds)
                    {
                        resultString = await _orderService.ProcessOrderAsync(model);

                        if (resultString == "success")
                        {
                            await _basketService.DeleteBasketAsync(buyerID);
                            return Ok(new { success = true, message = "Sipariş Başarıyla İşleme Alındı." });
                        }
                        else
                        {
                            return StatusCode(500, new { success = false, message = resultString });
                        }
                    }
                    else
                    {
                        resultString = await _orderService.Process3DsOrderAsync(model);

                        if (resultString != "ERROR")
                        {
                            await _basketService.DeleteBasketAsync(buyerID);
                            return Ok(new { success = true, message = resultString });
                        }
                        else
                        {
                            return StatusCode(500, new { success = false, message = "Sipariş Kaydedilirken Hata İle Karşılaşıldı.Lütfen Tekrar Deneyiniz." });
                        }
                    }
                }
                else
                {
                    return Ok(new { success = true, message = "Banka Transferi" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return StatusCode(500, new { success = false, message = "Sipariş Kaydedilirken Hata ile Karşılaşıldı. Lütfen Tekrar Deneyiniz." });
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CheckInstallment([FromBody] BinRequestDTO model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.BinNumber))
                    return BadRequest(new { success = false, message = "Kart Numarasý Boþ Olamaz." });

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
        public async Task<IActionResult> Subscribe(string emailAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(emailAddress))
                    return Ok(new { success = false, message = "Email adresi boþ olamaz." });

                // Validate email format
                if (!IsValidEmail(emailAddress))
                    return Ok(new { success = false, message = "Hatalý Email formatý." });

                if (_context.Subscribers.Any(s => s.EmailAddress == emailAddress))
                    return Ok(new { success = false, message = "Email abone listesinde mevcut." });

                // Save to the database
                var subscriber = new Subscriber { EmailAddress = emailAddress, SubscribedOn = DateTime.Now };
                _context.Subscribers.Add(subscriber);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Kayýt Baþarýlý!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Kayýt esnasýnda hata ile karþýlaþýldý. Lütfen daha sonra tekrar deneyiniz." });
            }
        }
        public async Task<IActionResult> SendEmail(string username,string emailAddress,string phone,string message,string subject)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                    return Ok(new { success = false, message = "Ýsim boþ olamaz." });
                if (string.IsNullOrEmpty(emailAddress))
                    return Ok(new { success = false, message = "Email adresi boþ olamaz." });
                // Validate email format
                if (!IsValidEmail(emailAddress))
                    return Ok(new { success = false, message = "Hatalý Email formatý." });
                if (string.IsNullOrEmpty(message))
                    return Ok(new { success = false, message = "Mesaj boþ olamaz." });


                

                return Ok(new { success = true, message = "Kayýt Baþarýlý!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Kayýt esnasýnda hata ile karþýlaþýldý.Lütfen daha sonra tekrar deneyiniz." });
            }
        }

        //-------------mail ends-------------//


        //-------------other pages -------------//
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult CookiePolicy()
        {
            return View();
        }
        public IActionResult IWannaMakeALongDistanceCall()
        {
            return View();
        }
        public IActionResult About()
        {
            authCookie = Request.Cookies["AuthToken"];
            if (authCookie != null)
                TempData["Cookie"] = "exists";
            else
                TempData["Cookie"] = "not exists";
            return View();
        }
        public IActionResult _PartialContactUs()
        {
            try
            {
                return PartialView("_PartialContactUs");
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        //-------------other pages ends -------------//

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private string RenderPartialViewToString(string viewName, object model)
        {
            this.ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                var viewEngine = this.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                var viewResult = viewEngine.FindView(this.ControllerContext, viewName, false);

                if (viewResult.Success == false)
                {
                    throw new Exception($"Ýlgili sayfa bulunamadý.");
                }

                var viewContext = new ViewContext(
                    this.ControllerContext,
                    viewResult.View,
                    this.ViewData,
                    this.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                viewResult.View.RenderAsync(viewContext).Wait();
                return writer.GetStringBuilder().ToString();
            }
        }
        public Dictionary<int, string> AddBasketCookie(string productCode, decimal quantity)
        {
            try
            {

                var cookieOptions = new CookieOptions();
                //returns result as int different product count in basket and cookie as string
                Dictionary<int, string> result = new();
                cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(30);
                cookieOptions.Path = "/";

                string? cookie = HttpContext.Request.Cookies["MyCart"];
                string myCart = string.Empty;
                if (cookie == null)
                {

                    result = UpdateCookieAddProduct(productCode, quantity, string.Empty);
                    if (result.Values.FirstOrDefault() == "HATA")
                    { 
                        return result; 
                    }

                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault(), cookieOptions);
                    return result;

                }
                else
                {
                    result = UpdateCookieAddProduct(productCode, quantity, cookie);

                    if (result.Values.FirstOrDefault() == "HATA")
                    { 
                        return result; 
                    }
                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault(), cookieOptions);

                    return result;

                }

            }
            catch (Exception)
            {

                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result; ;
            }
        }
        public Dictionary<int,string> UpdateCookie(string productCode, decimal quantity, string cookie)
        {
            try
            {
                bool exists = false;
                //how many items in cookie,cookie
                Dictionary<int, string> result = new();
                if (cookie == "")
                {
                    MyCart = productCode + ";" + quantity.ToString();
                    result.Add(1,MyCart);
                    return result;
                }
                else
                {
                    string[] MyCartArray = cookie.Split('&');
                    string updatedExistingID = string.Empty;
                    bool firstItem = true;
                    int totalProductCount = 0;
                    foreach (string item in MyCartArray)
                    {
                        string[] existingID = item.Split(';');

                        if (existingID[0] == productCode)
                        {
                            exists = true;
                            existingID[1] =  quantity.ToString();
                        }
                        //eðer ilk ürün ise & ekleme
                        if (firstItem)
                            updatedExistingID = updatedExistingID + string.Join(";", existingID);
                        else
                            updatedExistingID = updatedExistingID + "&" + string.Join(";", existingID);

                        firstItem = false;
                    }
                    if (!exists)
                    {
                        totalProductCount = MyCartArray.Count() + 1;
                        MyCart = cookie + "&" + productCode + ";" + quantity.ToString();
                        result.Add(totalProductCount,MyCart);
                        return result;
                    }

                    else
                    {
                        totalProductCount = MyCartArray.Count();
                        MyCart = updatedExistingID;
                        result.Add(totalProductCount, MyCart);
                        return result;
                    }
                }
            }
            catch
            {
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result;
            }
        }
        public Dictionary<int,string> UpdateCookieAddProduct(string productCode, decimal quantity, string cookie)
        {
            try
            {
                bool exists = false;
                //how many items in cookie,cookie
                Dictionary<int, string> result = new();
                if (cookie == "")
                {
                    MyCart = productCode + ";" + quantity.ToString();
                    result.Add(1,MyCart);
                    return result;
                }
                else
                {
                    string[] MyCartArray = cookie.Split('&');
                    string updatedExistingID = string.Empty;
                    bool firstItem = true;
                    int totalProductCount = 0;
                    foreach (string item in MyCartArray)
                    {
                        string[] existingID = item.Split(';');

                        if (existingID[0] == productCode)
                        {
                            exists = true;
                            existingID[1] = (Convert.ToDecimal(existingID[1]) + quantity).ToString();
                        }
                        //eðer ilk ürün ise & ekleme
                        if (firstItem)
                            updatedExistingID = updatedExistingID + string.Join(";", existingID);
                        else
                            updatedExistingID = updatedExistingID + "&" + string.Join(";", existingID);

                        firstItem = false;
                    }
                    if (!exists)
                    {
                        totalProductCount = MyCartArray.Count() + 1;
                        MyCart = cookie + "&" + productCode + ";" + quantity.ToString();
                        result.Add(totalProductCount,MyCart);
                        return result;
                    }

                    else
                    {
                        totalProductCount = MyCartArray.Count();
                        MyCart = updatedExistingID;
                        result.Add(totalProductCount, MyCart);
                        return result;
                    }
                }
            }
            catch
            {
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result;
            }
        }
        public Dictionary<int, string> RemoveCookie(string productCode, string cookie)
        {
            try
            {
                bool found = false;
                //how many items in cookie,cookie
                Dictionary<int, string> result = new();
               
                string[] MyCartArray = cookie.Split('&');
                string removedVersion = string.Empty;
                bool firstItem = true;
                int totalProductCount = 0;
                foreach (string item in MyCartArray)
                {
                    string[] existingID = item.Split(';');

                    if (existingID[0] == productCode)
                        found = true;
                    
                    //eðer ilk ürün ise & ekleme
                    if (firstItem && !found)
                        removedVersion = removedVersion + string.Join(";", existingID);
                    else if (string.IsNullOrEmpty(removedVersion) && !found)
                        removedVersion = string.Join(";", existingID);
                    else if (!found)
                        removedVersion = removedVersion + "&" + string.Join(";", existingID);

                    firstItem = false;
                    found = false;
                }
                
                    totalProductCount = MyCartArray.Count() - 1;
                    result.Add(totalProductCount, removedVersion);
                    return result;
                
            }
            catch
            {
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result;
            }
        }
        public Dictionary<string, int> GetProductsFromCookie(string cookie)
        {
            try
            {
                string[] MyCartArray = cookie.Split('&');
                Dictionary<string, int> result = new();
                string productCode = string.Empty;
                int quantity = 0;
                foreach (string item in MyCartArray)
                {
                    string[] existingID = item.Split(';');

                    if (string.IsNullOrEmpty(existingID[0]))
                        productCode = string.Empty;
                    else
                        productCode = existingID[0];
                    if (Int32.TryParse(existingID[1],out quantity) == false)
                        quantity = 0;
                    else
                        quantity = Convert.ToInt32(existingID[1]);

                    result.Add(productCode, quantity);
                    
                }
                return result;
                
            }
            catch
            {
                Dictionary<string, int> result = new();
                result.Add("HATA", 0);
                return result;
            }
        }
        
        public string GetFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : string.Empty;
        }
        public string GetLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[^1] : string.Empty;
        }


    }
}
