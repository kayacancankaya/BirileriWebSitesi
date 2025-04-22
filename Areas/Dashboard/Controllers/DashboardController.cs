using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BirileriWebSitesi.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Route("Dashboard/[controller]/[action]")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
