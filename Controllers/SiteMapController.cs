using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BirileriWebSitesi.Controllers
{
    [Route("sitemap.xml")]
    public class SiteMapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SiteMapController> _logger;
        public SiteMapController(ApplicationDbContext context, IConfiguration configuration, ILogger<SiteMapController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? baseUrl = string.Empty;
            if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
                baseUrl = $"{Request.Scheme}://{Request.Host}";
            else
                baseUrl = "https://birilerigt.com/";

            _logger.LogError("Sitemap baseUrl: " + baseUrl);


            XNamespace ns = "https://www.sitemaps.org/schemas/sitemap/0.9";
            var sitemap = new XElement(ns + "urlset");

            // 1. Add static pages
            var staticPages = new[]
            {
            "/", "/Home/ContactUs","/Home/About","/Cart","/Cart/Inquiry","/Blog","/Home/Privacy","/Home/CookiePolicy","/Identity/Account/Login","/Identity/Account/Register","/Identity/Account/ForgotPassword","/Identity/Account/ResetPassword",
            "/Identity/Account/Manage","/Identity/Account/Manage/Email","/Identity/Account/Manage/ChangePassword","/Identity/Account/Manage/ExternalLogins","/Identity/Account/Manage/PersonalData","/Identity/Account/Manage/DeletePersonalData",
            "/Home/DistanceSellingAgreement","/Home/KVKK","/Home/NotFound","/Order/Checkout","/Shop"
            };

            foreach (var page in staticPages)
            {
                sitemap.Add(
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}{page}"),
                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.7")
                    )
                );
            }
            _logger.LogError("Sitemap after static pages: " + sitemap.ToString());
            // 2. Add dynamic products or articles from DB
            var products = await _context.Products
                                                .Include(p => p.ProductVariants)
                                                .ToListAsync(); // Example: products table
            if(products == null)
            {
                if(products.Count  > 0)
                {
                    foreach (var product in products)
                    {
                        if (!string.IsNullOrEmpty(product.ProductCode))
                        {
                            if (product.ProductVariants == null)
                                continue;
                            if (product.ProductVariants.Count == 0)
                                continue;
                            foreach (var variant in product.ProductVariants)
                            {
                                if (!string.IsNullOrEmpty(variant.ProductCode))
                                {
                                    sitemap.Add(
                                    new XElement(ns + "url",
                                        new XElement(ns + "loc", $"{baseUrl}/Shop/ProductDetailed?productCode={variant.ProductCode}"),
                                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                                        new XElement(ns + "changefreq", "weekly"),
                                        new XElement(ns + "priority", "0.9")
                                        )
                                    );
                                }
                            }
                        }
                    }
                }
                    
            }
            _logger.LogError("Sitemap after products: " + sitemap.ToString());
            // 3. Add dynamic blog posts from DB
            //var allBlogPosts = await _context.BlogPosts.ToListAsync();
            //if(allBlogPosts != null)
            //{
            //    if(allBlogPosts.Count > 0)
            //    {
            //        foreach (var post in allBlogPosts)
            //        {
            //            sitemap.Add(
            //                new XElement(ns + "url",
            //                    new XElement(ns + "loc", $"{baseUrl}/blog/BlogPost?path={post.Slug}"), // matches your BlogPost(string path)
            //                    new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            //                    new XElement(ns + "changefreq", "weekly"),
            //                    new XElement(ns + "priority", "0.9")
            //                )
            //            );
            //        }
            //    }
            //}
            //_logger.LogError("Sitemap after blog posts: " + sitemap.ToString());

            // 🔹 4. Add dynamic catalog pages
            var catalogs = await _context.Catalogs.ToListAsync();
            if(catalogs != null)
            {
                if(catalogs.Count > 0)
                {
                    foreach (var catalog in catalogs)
                    {
                        if (string.IsNullOrEmpty(catalog.CatalogName))
                            continue;
                        // Important: URL-encode catalogName for safe links
                        var encodedName = Uri.EscapeDataString(catalog.CatalogName);

                        sitemap.Add(
                            new XElement(ns + "url",
                                new XElement(ns + "loc", $"{baseUrl}/Shop/Catalog?catalogName={encodedName}"),
                                new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                                new XElement(ns + "changefreq", "weekly"),
                                new XElement(ns + "priority", "0.8")
                            )
                        );
                    }
                }
            }
            _logger.LogError("Sitemap after catalogs: " + sitemap.ToString());
            var xml = new XDocument(sitemap);
            return Content(xml.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}