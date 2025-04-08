using BirileriWebSitesi.Data;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account.Manage
{
    public class OrderDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderDetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Order Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Order = await _context.Orders
                .Include(o => o.OrderItems) 
                .ThenInclude(p => p.ProductVariant) 
                .FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == user.Id);

            if (Order == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
