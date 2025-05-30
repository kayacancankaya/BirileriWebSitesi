using BirileriWebSitesi.Data;
using BirileriWebSitesi.Models.ViewModels;
using BirileriWebSitesi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BirileriWebSitesi.Controllers
{
    public class ShopController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        public ShopController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
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
                                            .Include(d => d.Discounts)
                                             .OrderByDescending(p => p.Popularity)
                                             .ToListAsync();
                //get total products
                pagination.TotalCount = await _context.Products.Where(a => a.IsActive == true).CountAsync();
                if (products == null)
                    return RedirectToAction("NotFound","Home");

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
                return RedirectToAction("NotFound","Home");
            }
        }
        public async Task<IActionResult> ShopFiltered(int catalogID, string searchFilter, int pageNumber, decimal minPrice, decimal maxPrice)
        {
            try
            {
                IEnumerable<Product> products = new List<Product>();
                int totalCount = 0;
                int totalPage = 0;
                IQueryable<Product> query = _context.Products
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
        public async Task<IActionResult> ProductDetailed(string productCode)
        {
            try
            {
                if (string.IsNullOrEmpty(productCode))
                    return RedirectToAction("NotFound", "Home");
                Product? product;
                bool isBaseProduct = false;
                int counter = 0;
                string? initialVariantName = string.Empty;
                decimal variantPrice = decimal.Zero;
                string? selectedVariantAttribute = string.Empty;
                //if it is not base / bundle base product get its base product and its name 
                if (productCode.Length>12)
                {
                    string? baseProduct = await _context.ProductVariants.Where(c => c.ProductCode == productCode)
                                                        .Select(b=>b.BaseProduct)
                                                        .FirstOrDefaultAsync();

                    product = await _context.Products.Where(c => c.ProductCode == baseProduct)
                                                       .Include(v => v.ProductVariants)
                                                       .FirstOrDefaultAsync();

                    initialVariantName = await _context.ProductVariants.Where(c => c.ProductCode == productCode)
                                                        .Select(n => n.ProductName)
                                                        .FirstOrDefaultAsync();
                    variantPrice = await _context.ProductVariants.Where(c => c.ProductCode == productCode)
                                                                    .Select(p => p.Price)
                                                                    .FirstOrDefaultAsync();
                    if (productCode.EndsWith("B"))
                        selectedVariantAttribute = productCode.Substring(11, productCode.Length - 12);
                    else
                        selectedVariantAttribute = productCode.Substring(11, productCode.Length - 11);
                }
                // if it is base product
                else
                {
                    product = await _context.Products.Where(c => c.ProductCode == productCode)
                                                        .Include(v => v.ProductVariants)
                                                        .FirstOrDefaultAsync();
                    isBaseProduct = true;
                    variantPrice = product.ProductVariants.FirstOrDefault().Price;
                }

                if (product == null)
                    return RedirectToAction("NotFound","Home");

                //if the product is a base, we need to get the variant code
                string productVariant = productCode;
                if(isBaseProduct)
                {
                    productVariant = product.ProductVariants.OrderBy(c => c.ProductCode).FirstOrDefault().ProductCode;
                    if (productVariant.EndsWith("B"))
                        selectedVariantAttribute = productVariant.Substring(11, productVariant.Length - 12);
                    else
                        selectedVariantAttribute = productVariant.Substring(11, productVariant.Length - 11);
                }

                Dictionary<string, string> globalVariants = new Dictionary<string, string>();
                string? variantKey = string.Empty;
                string? variantValue = string.Empty;
                Dictionary<string, string> variantAttributes = new Dictionary<string, string>();
                string? variantAttribute = string.Empty;
                string? variantAttributeValue = string.Empty;
                for (int i = 11; i < productVariant.Length; i += 6)
                {
                    if (productVariant.Substring(productVariant.Length - 1, 1) == "B" &&
                        i + 1 == productVariant.Length)
                        break;

                    //fetch global variants
                    variantKey = productVariant.Substring(i, 3);
                    variantValue = await _context.Variants.Where(v => v.VariantCode == variantKey).Select(n => n.VariantName).FirstOrDefaultAsync();
                    if (!string.IsNullOrEmpty(variantValue) &&
                        !globalVariants.ContainsKey(variantKey))
                        globalVariants.Add(variantKey, variantValue);
                    //fetch variant attributes of global variant 
                    counter = 0;
                    foreach (var variant in product.ProductVariants)
                    {
                        variantAttribute = variant.ProductCode.Substring(i + 3, 3);
                        variantAttributeValue = await _context.VariantAttributes.Where(v => v.VariantCode == variantKey &&
                                                                                    v.VariantAttributeCode == variantAttribute)
                                                                            .Select(n => n.VariantAttributeName).FirstOrDefaultAsync();


                        //if base product calling the page get first names of first variants to display initializing page
                        //else use already defined
                        if(counter == 0 && isBaseProduct)
                            initialVariantName = string.Format("{0} {1} {2}", product.ProductName , initialVariantName, variantAttributeValue);
                        counter++;
                        
                        if (!string.IsNullOrEmpty(variantAttributeValue) &&
                            !variantAttributes.ContainsKey(variantKey + variantAttribute))
                            variantAttributes.Add(variantKey + variantAttribute, variantAttributeValue);
                        
                    }
                }
                //get variant info to display initializing page
                ProductDetailedVariantInfoViewModel initialVariantInfo = new();
                initialVariantInfo.VariantCode = productVariant;
                initialVariantInfo.VariantName =  initialVariantName;
                initialVariantInfo.VariantPrice = variantPrice;
                initialVariantInfo.SelectedVariantAttribute = selectedVariantAttribute;
                //get image path of variant to display initializing page
                ProductDetailedVariantImageViewModel initialVariantImage = new();
                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"); 
                string basePath;

                if (environment == "Production")
                    basePath = "https://birilerigt.com/wwwroot";
                else
                    basePath = "C:\\Users\\kayac\\OneDrive\\Desktop\\1-c#\\appDev\\BirileriWebSitesi\\wwwroot";

                

                string? imagePath = await _context.ProductVariants.Where(p => p.ProductCode == productVariant)
                                                                    .Select(i => i.ImagePath)
                                                                    .FirstOrDefaultAsync();
                                                                    
                string folderPath = Path.Combine(basePath, "images", "resource", "products", imagePath);
                string folderUrlPath = $"/images/resource/products/{imagePath}/";
                List<string> imagePaths = new List<string>();
            
                    if (Directory.Exists(folderPath))
                    {
                        var files = Directory.GetFiles(folderPath)
                            .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") || 
                                        f.EndsWith(".png") || f.EndsWith(".avif"))
                            .OrderBy(f => f) // optional: sort by name
                            .ToList();
            
                        imagePaths = files.Select(f =>
                            folderUrlPath + Path.GetFileName(f)).ToList();
                    }
                
                initialVariantImage.FilePaths = imagePaths;
                initialVariantImage.ProductVariantName = string.Format("{0},{1}", product.ProductName, initialVariantName);

                //get related products
                List<string> relatedProductCodes = await _context.RelatedProducts.Where(c => c.ProductCode == productCode)
                                                                                 .Select(r => r.RelatedProductCode)
                                                                                 .ToListAsync();
                List<Product> relatedProducts = new List<Product>();

                relatedProducts = await _context.Products
                                        .Where(p => relatedProductCodes.Contains(p.ProductCode))
                                        .ToListAsync();

                IEnumerable<Product> popularProducts = await _context.Products.Where(a => a.IsActive == true)
                                                                        .OrderByDescending(p => p.Popularity)
                                                                        .Take(3)
                                                                        .ToListAsync();

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
                return RedirectToAction("NotFound","Home");
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
                    return BadRequest(new { success = false, message = "Varyant Seçeneği Bulunamadı." });
                }
                string variantCode = productCode;
                string variantName = productName;
                foreach (var item in variantAttributes)
                {
                    variantCode = variantCode + item.Key;
                    variantName = variantName + " " + item.Value;
                }
                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"); 
                string basePath;
                
                if (environment == "Production")
                    basePath = "https://birilerigt.com/wwwroot"; 
                else
                    basePath = basePath = "C:\\Users\\kayac\\OneDrive\\Desktop\\1-c#\\appDev\\BirileriWebSitesi\\wwwroot";  
              
                string? imagePath = await _context.ProductVariants.Where(p => p.ProductCode == variantCode)
                                                                    .Select(i => i.ImagePath)
                                                                    .FirstOrDefaultAsync();
                                                                    
                string folderPath = Path.Combine(basePath, "images", "resource", "products", imagePath);
                string folderUrlPath = $"/images/resource/products/{imagePath}/";
                List<string> imagePaths = new List<string>();
            
                    if (Directory.Exists(folderPath))
                    {
                        var files = Directory.GetFiles(folderPath)
                            .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") || 
                                        f.EndsWith(".png") || f.EndsWith(".avif"))
                            .OrderBy(f => f) // optional: sort by name
                            .ToList();
            
                        imagePaths = files.Select(f =>
                            folderUrlPath + Path.GetFileName(f)).ToList();
                    }
                
                ProductDetailedVariantImageViewModel imageModel = new();
                imageModel.FilePaths = imagePaths;
                imageModel.ProductVariantName = variantName;
                decimal variantPrice = await _context.ProductVariants.Where(v => v.ProductCode == variantCode)
                                                                     .Select(p => p.Price)
                                                                     .FirstOrDefaultAsync();

                ProductDetailedVariantInfoViewModel infoModel = new();
                infoModel.VariantCode = variantCode;
                infoModel.VariantName = variantName;
                infoModel.VariantPrice = variantPrice;


                var imagePartial = await RenderPartialViewToStringAsync("_PartialProductVariantImage", imageModel);
                var infoPartial = await RenderPartialViewToStringAsync("_PartialProductVariantInfo", infoModel);

                // Return JSON response with both partial views
                return Json(new { imagePartial, infoPartial });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return BadRequest(new { success = false, message = "Beklenmeyen Bir Hata Oluştu" });
            }
        }
        private async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
        {
            ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewEngine = HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                var viewResult = viewEngine.FindView(ControllerContext, viewName, false);

                if (!viewResult.Success)
                {
                    throw new Exception("İlgili sayfa bulunamadı.");
                }

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }
        public async Task<IActionResult> Catalog(string catalogName)
        {
            try
            {
                if(string.IsNullOrEmpty(catalogName))
                {
                    return RedirectToAction("NotFound","Home");
                }
                IEnumerable<Product> products = new List<Product>();
                int totalCount = 0;
                int totalPage = 0;
                int pageNumber = 1;
                int catalogID = await _context.Catalogs.Where(_context => _context.CatalogName == catalogName)
                                                .Select(c => c.Id)
                                                .FirstOrDefaultAsync();
                // get products
                products = await _context.Products
                                     .Where(n => n.CatalogId == catalogID &&
                                                 n.IsActive == true)
                                     .Skip((pageNumber - 1) * PaginationViewModel.PageSize)
                                     .Take(PaginationViewModel.PageSize)
                                     .Include(d => d.Discounts)
                                     .ToListAsync();

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
                totalCount = await _context.Products
                                     .Where(n => n.CatalogId == catalogID &&
                                                 n.IsActive == true)
                                     .CountAsync();

                totalPage = (int)Math.Ceiling((double)totalCount / PaginationViewModel.PageSize);

                ProductCardViewModel model = new();
                model.products = products;
                PaginationViewModel pagination = new();
                pagination.TotalCount = totalCount;
                pagination.TotalPage = totalPage;
                pagination.CurrentPage = pageNumber;
                model.pagination = pagination;


                //get all categories
                IEnumerable<Catalog> catalogs = await _context.Catalogs.ToListAsync();

                if (products == null)
                    return RedirectToAction("NotFound","Home");

                //get popular products
                IEnumerable<Product> popularProducts = await _context.Products.Where(a => a.IsActive == true)
                                                                        .OrderByDescending(p => p.Popularity)
                                                                        .Take(3)
                                                                        .ToListAsync();


                ShopViewModel shop = new ShopViewModel();
                ProductCardViewModel productCard = new ProductCardViewModel();
                productCard.products = products;
                productCard.pagination = pagination;
                shop.productCard = productCard;
                shop.Catalogs = catalogs;
                shop.PopularProducts = popularProducts;

                return View("Index", shop);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return RedirectToAction("NotFound","Home");
            }
        }
    }
}
