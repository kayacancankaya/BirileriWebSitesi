using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.OrderAggregate;
using BirileriWebSitesi.Services;
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
        private readonly ILogger<OrderDetailsModel> _logger;
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;

        public OrderDetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<OrderDetailsModel> logger,
                                  IOrderService orderService, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _orderService = orderService;
            _emailService = emailService;
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
                .Where(o => o.Id == id && o.BuyerId == user.Id
                        && o.Status>0)
                .Include(o => o.OrderItems) 
                .ThenInclude(p => p.ProductVariant) 
                .FirstOrDefaultAsync();

            if (Order == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCancelOrderAsync(int orderId)
        {
            if (!ModelState.IsValid)
            {
                return Redirect("/Identity/Account/Manage/Index");
            }

            try
            {

                string userID = string.Empty;
                IdentityUser? user = new();

                if (!User.Identity.IsAuthenticated)
                {
                    TempData["DangerMessage"] = "Kullan�c� Bulunamad�.";
                    return Redirect("/Identity/Account/Manage/Index");
                }
                userID = _userManager.GetUserId(User);
                user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                string email = user.Email;
                Order order = await _orderService.GetOrderAsync(orderId);

                int resultInt = await _orderService.CancelOrderAsync(orderId);
                
                if (resultInt == -1)
                {
                    TempData["DangerMessage"] = "Sipari� �ptal Edilirken Hata ile Kar��la��ld�. \n" +
                                                 "L�tfen �leti�im Kutusundan Bizimle �leti�ime Ge�iniz.";
                }
                else if (resultInt == 0)
                {
                    TempData["DangerMessage"] = "�lgili Sipari� Sistemde Bulunamad�. \n" +
                                                 "L�tfen �leti�im Kutusundan Bizimle �leti�ime Ge�iniz.";
                }
                else if (resultInt == 1)
                {
                    TempData["SuccessMessage"] = "Sipari�iniz �ptal Edildi.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Not Recieved");
                }
                else if (resultInt == 2)
                {
                    TempData["SuccessMessage"] = "Sipari� �ptal Talebiniz Al�nd�.\n�r�nler Taraf�m�za Ula�t���nda �cret �adesi Sa�lanacakt�r.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Recieved");
                }
                else if (resultInt == 3)
                {
                    TempData["DangerMessage"] = "�deme iade edilirken hata ile kar��la��ld�. \n" +
                                                 "L�tfen �leti�im Kutusundan Bizimle �leti�ime Ge�iniz.";
                }

                else if (resultInt == 4)
                {
                    TempData["SuccessMessage"] = "Sipari�iniz �ptal Edildi.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Not Recieved");
                }

                else if (resultInt == 5)
                {
                    TempData["SuccessMessage"] = "Sipari� �ptal Talebiniz Al�nd�.\n�r�nler Taraf�m�za Ula�t���nda �cret �adesi Sa�lanacakt�r.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Recieved");
                }
                return Redirect("/Identity/Account/Manage/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                TempData["DangerMessage"] = "Sipari� �ptal Edilirken Hata ile Kar��la��ld�";
                return Redirect("/Identity/Account/Manage/Index");
            }
        }
        // POST handler for CancelOrder
        public async Task<IActionResult> OnPostCancelOrderItemAsync(int orderId, string productCode)
        {
            if (!ModelState.IsValid)
            {
                return Redirect("/Identity/Account/Manage/Index");
            }

            try
            {
                string userID = string.Empty;
                IdentityUser? user = new();

                if (!User.Identity.IsAuthenticated)
                {
                    TempData["DangerMessage"] = "Kullan�c� Bulunamad�.";
                    return Redirect("/Identity/Account/Manage/Index");
                }
                userID = _userManager.GetUserId(User);
                user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                string? email = user.Email;
                Order order = await _orderService.GetOrderAsync(orderId);
                OrderItem? item = order.OrderItems.FirstOrDefault(i => i.ProductCode == productCode);

                int resultInt = await _orderService.CancelOrderItemAsync(orderId, productCode);

                if (resultInt == -1)
                {
                    TempData["DangerMessage"] = "Sipari� �ptal Edilirken Hata ile Kar��la��ld�. \n" +
                                                 "L�tfen �leti�im Kutusundan Bizimle �leti�ime Ge�iniz.";
                }
                else if (resultInt == 0)
                {
                    TempData["DangerMessage"] = "�lgili Sipari� Sistemde Bulunamad�. \n" +
                                                 "L�tfen �leti�im Kutusundan Bizimle �leti�ime Ge�iniz.";
                }
                else if (resultInt == 1)
                {
                    TempData["SuccessMessage"] = "Sipari�iniz �ptal Edildi.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order,item, email, "Products Not Recieved");
                }
                else if (resultInt == 2)
                {
                    TempData["SuccessMessage"] = "24 saat i�erisinde yap�lan �demeler sadece tamamen iptal edilebilir.\n" +
                                                "Sipari�i iptal edebilir ya da �r�n bazl� �r�n iptali i�in 24 saatin dolmas�n� bekleyebilirsiniz.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order,item, email, "Products Not Recieved");
                }
                else if (resultInt == 3)
                {
                    TempData["DangerMessage"] = "�r�n tesliminden 15 g�n sonra �r�n iadesi kabul edilememektedir. \n" +
                                                 "L�tfen �leti�im Kutusundan Bizimle �leti�ime Ge�iniz.";
                }

                else if (resultInt == 4)
                {
                    TempData["SuccessMessage"] = "Sipari�iniz �ptal Edildi.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order, item, email, "Products Not Recieved");
                }

                else if (resultInt == 5)
                {
                    TempData["SuccessMessage"] = "Sipari� �ptal Talebiniz Al�nd�.\n�r�nler Taraf�m�za Ula�t���nda �cret �adesi Sa�lanacakt�r.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order, item, email, "Products Recieved");
                }
                return Redirect("/Identity/Account/Manage/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                TempData["DangerMessage"] = "Sipari� �ptal Edilirken Hata ile Kar��la��ld�";
                return Redirect("/Identity/Account/Manage/Index");
            }
        }
    }
}
