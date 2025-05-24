using Microsoft.AspNetCore.Mvc;

namespace BirileriWebSitesi.Controllers
{
    public class BasketController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
