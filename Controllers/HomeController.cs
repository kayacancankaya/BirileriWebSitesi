using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.OrderAggregate;
using BirileriWebSitesi.Models.ViewModels;
using BirileriWebSitesi.Services;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Protocol.Core.Types;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
namespace BirileriWebSitesi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private string? authCookie = string.Empty;
        string MyCart = string.Empty;
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger,
                               IServiceProvider serviceProvider )
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        
        public IActionResult Index()
        {
            try
            {
                authCookie = Request.Cookies[".AspNetCore.Identity.Application"];
                if (authCookie != null)
                    TempData["Cookie"] = "exists";
                else
                    TempData["Cookie"] = "not exists";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
            }
        }
        //-------------shop-------------//
        public async Task<IActionResult> Shop()
        {
            try
            {

                //get all categories
                IEnumerable<Catalog> catalogs = await _context.Catalogs.ToListAsync();
                //initiate view model
                PaginationViewModel pagination = new PaginationViewModel();
                IEnumerable<Product> products = new List<Product>();
                // get products
                products = await _context.Products.Where(a => a.IsActive == true)
                                            .Take(PaginationViewModel.PageSize)
                                            .Include(d=>d.Discounts)
                                             .OrderByDescending(p=>p.Popularity)
                                             .ToListAsync();
                //get total products
                pagination.TotalCount = await _context.Products.Where(a => a.IsActive == true).CountAsync();
                if(products == null)
                   return View("NotFound");

                //get popular products
                IEnumerable<Product> popularProducts = await _context.Products
                                                      .Where(p => p.IsActive)
                                                      .OrderByDescending(p => p.Popularity)
                                                      .Take(3)
                                                      .ToListAsync();

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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
               return View("NotFound");
            }
        }
        public async Task<IActionResult> ShopFiltered(int catalogID, string searchFilter, int pageNumber, decimal minPrice, decimal maxPrice)
        {
            try
            {
                IEnumerable<Product> products = new List<Product>();
                int totalCount = 0;
                int totalPage = 0;
                IQueryable<Product> query =  _context.Products
                                                    .Where(n => n.BasePrice >= minPrice && n.BasePrice <= maxPrice &&
                                                                (catalogID == 0 || n.CatalogId == catalogID) &&
                                                                n.IsActive == true);

                if (!string.IsNullOrEmpty(searchFilter))
                {
                    //string loweredFilter = searchFilter.ToLower();
                    query = query.Where(n => EF.Functions.Like(n.ProductName, $"%{searchFilter}%"));

                }

                products = await query
                    .OrderBy(n => n.ProductCode) // ensure stable ordering before Skip/Take
                    .Skip((pageNumber - 1) * PaginationViewModel.PageSize)
                    .Take(PaginationViewModel.PageSize)
                    .Include(d => d.Discounts)
                    .ToListAsync();

                //get total products
                totalCount = await query
                                .OrderBy(n => n.ProductCode) // ensure stable ordering before Skip/Take
                                .CountAsync();
                //filter related discounts

                foreach (Product product in products)
                {
                    product.Discounts = product.Discounts.Where(d => d.StartDate <= DateTime.Now &&
                                                                        d.EndDate >= DateTime.Now)
                                                            .OrderByDescending(d => d.StartDate)
                                                            .ToList();
                    if (product.Discounts == null)
                        product.Discounts = new List<Discount>();
                }

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
                return StatusCode(500, new { success = false, message = "Filtreleme işlemi esnasında hata ile Karşılaşıldı.Lütfen daha sonra tekrar deneyiniz." });
            }
        }
        public IActionResult ProductDetailed(string productCode)
        {
            try
            {
                //get product
                Product? product = _context.Products.Where(c => c.ProductCode == productCode)
                                                    .Include(v => v.ProductVariants)
                                                    .FirstOrDefault();

                if (product == null)
                    return View("NotFound");

                string productVariant = product.ProductVariants.OrderBy(c => c.ProductCode).FirstOrDefault().ProductCode;
                Dictionary<string, string> globalVariants = new Dictionary<string, string>();
                string? variantKey = string.Empty;
                string? variantValue = string.Empty;
                Dictionary<string, string> variantAttributes = new Dictionary<string, string>();
                string? variantAttribute = string.Empty;
                string? variantAttributeValue = string.Empty;
                int counter = 0;
                string initialVariantName = string.Empty;
                for (int i = 11; i < productVariant.Length; i += 6)
                {
                    if (productVariant.Substring(productVariant.Length - 1, 1) == "B" &&
                        i + 1 == productVariant.Length)
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
                        if (counter == 0)
                        {
                            initialVariantName = string.Format("{0} {1}", initialVariantName, variantAttributeValue);
                            counter++;
                        }
                        if (!string.IsNullOrEmpty(variantAttributeValue) &&
                            !variantAttributes.ContainsKey(variantKey + variantAttribute))
                            variantAttributes.Add(variantKey + variantAttribute, variantAttributeValue);
                    }
                }
                //get variant info to display initializing page
                ProductDetailedVariantInfoViewModel initialVariantInfo = new();
                initialVariantInfo.VariantName = product.ProductName + " " + initialVariantName;
                initialVariantInfo.VariantPrice = product.ProductVariants.FirstOrDefault().Price;
                //get image path of variant to display initializing page
                ProductDetailedVariantImageViewModel initialVariantImage = new();
                initialVariantImage.FilePath = string.Format("images/resource/products/{0}/1.jpg", product.ProductVariants.First().ImagePath);
                initialVariantImage.ProductVariantName = string.Format("{0},{1}", product.ProductName, initialVariantName);

                //get related products
                List<string> relatedProductCodes = _context.RelatedProducts.Where(c => c.ProductCode == productCode).Select(r => r.RelatedProductCode).ToList();
                List<Product> relatedProducts = new List<Product>();

                relatedProducts = _context.Products
                                        .Where(p => relatedProductCodes.Contains(p.ProductCode))
                                        .ToList();

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
                return View("NotFound");
            }
        }
        public IActionResult _PartialProductCard(IEnumerable<Product> products)
        {
            try
            {

                if (products == null)
                    return Ok(new { success = false, message = "Ürün Bulunamadı." });
                if (!products.Any())
                    return Ok(new { success = false, message = "Ürün Bulunamadı." });


                return PartialView("_PartialProductCard", products);

            }
            catch (Exception)
            {
                return Ok(new { success = false, message = "Ürün Listelenirken Hata ile Karşılaşıldı." });
            }
        }
        public async Task<IActionResult> GetPartialViewsForProductVariant(string productCode, string productName, Dictionary<string, string> variantAttributes)
        {
            try
            {
                if (string.IsNullOrEmpty(productCode) ||
                    string.IsNullOrEmpty(productName) ||
                    variantAttributes == null)
                {
                    return BadRequest(new { success = false, message = "Varyant Seçeneði Bulunamadı." });
                }
                string variantCode = productCode;
                string variantName = productName;
                foreach (var item in variantAttributes)
                {
                    variantCode = variantCode + item.Key;
                    variantName = variantName + " " + item.Value;
                }
                string imagePath = await _context.ProductVariants.Where(v => v.ProductCode == variantCode).Select(f => f.ImagePath).FirstOrDefaultAsync();
                string filePath = string.Format("images/resource/products/{0}/1.jpg",
                                                imagePath);

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
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest(new { success = false, message = "Beklenmeyen Bir Hata Oluştu" });
            }
        }
        public IActionResult Catalog(int catalogID)
        {
            try
            {
                IEnumerable<Product> products = new List<Product>();
                int totalCount = 0;
                int totalPage = 0;
                int pageNumber = 1;
                // get products
                products = _context.Products
                                     .Where(n => n.CatalogId == catalogID &&
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
                    if (product.Discounts == null)
                        product.Discounts = new List<Discount>();
                }
                //get total products
                totalCount = _context.Products
                                     .Where(n => n.CatalogId == catalogID &&
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


                //get all categories
                IEnumerable<Catalog> catalogs = _context.Catalogs.ToList();

                if (products == null)
                    return View("NotFound");

                //get popular products
                IEnumerable<Product> popularProducts = _context.Products.OrderByDescending(p => p.Popularity)
                                                                        .Where(a => a.IsActive == true)
                                                                         .Take(3);


                ShopViewModel shop = new ShopViewModel();
                ProductCardViewModel productCard = new ProductCardViewModel();
                productCard.products = products;
                productCard.pagination = pagination;
                shop.productCard = productCard;
                shop.Catalogs = catalogs;
                shop.PopularProducts = popularProducts;

                return View("Shop", shop);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return View("NotFound");
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
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest();
            }
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

        //-------------mail ends-------------//


        //-------------other pages -------------//
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
        public IActionResult IWannaMakeALongDistanceCall()
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
                authCookie = Request.Cookies["AuthToken"];
                if (authCookie != null)
                    TempData["Cookie"] = "exists";
                else
                    TempData["Cookie"] = "not exists";
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult NotFound()
        {
            return View();
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
                    throw new Exception($"Ýlgili sayfa bulunamadı.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                //returns result as int different product count in basket and cookie as string
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                //returns result as int different product count in basket and cookie as string
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
