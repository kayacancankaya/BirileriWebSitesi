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
                Dictionary<string, string> Catalogs = new Dictionary<string, string>();
                Catalogs.Add("Spor Malzemeleri", "spor-malzemeleri");
                Catalogs.Add("Ofis ve Kırtasiye Ürünleri", "ofis-kirtasiye-urunleri");
                Catalogs.Add("Pet Shop Ürünleri", "pet-shop-urunleri");
                Catalogs.Add("Ev Gereçleri", "ev-gerecleri");
                Catalogs.Add("Elektronik Ürünler", "elektronik-urunler");
                Catalogs.Add("Oyuncak & Hobi Ürünleri", "oyuncak-hobi-urunleri");
                
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



    }
}
