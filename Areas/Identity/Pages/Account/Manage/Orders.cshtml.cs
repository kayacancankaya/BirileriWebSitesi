using BirileriWebSitesi.Data;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account.Manage
{
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Order> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {

                ViewData["ActivePage"] = ManageNavPages.Orders;
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                Orders = await _context.Orders
                    .Where(o => o.BuyerId == user.Id &&
                                o.Status > 0 )
                    .OrderByDescending(o => o.OrderDate)
                    .Include(o=>o.OrderItems)
                    .ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
    }
}
