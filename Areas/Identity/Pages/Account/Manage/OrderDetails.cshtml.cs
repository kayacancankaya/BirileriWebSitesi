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
                    TempData["DangerMessage"] = "Kullanýcý Bulunamadý.";
                    return Redirect("/Identity/Account/Manage/Index");
                }
                userID = _userManager.GetUserId(User);
                user = await _userManager.Users.Where(i => i.Id == userID).FirstOrDefaultAsync();
                string email = user.Email;
                Order order = await _orderService.GetOrderAsync(orderId);

                int resultInt = await _orderService.CancelOrderAsync(orderId);
                
                if (resultInt == -1)
                {
                    TempData["DangerMessage"] = "Sipariþ Ýptal Edilirken Hata ile Karþýlaþýldý. \n" +
                                                 "Lütfen Ýletiþim Kutusundan Bizimle Ýletiþime Geçiniz.";
                }
                else if (resultInt == 0)
                {
                    TempData["DangerMessage"] = "Ýlgili Sipariþ Sistemde Bulunamadý. \n" +
                                                 "Lütfen Ýletiþim Kutusundan Bizimle Ýletiþime Geçiniz.";
                }
                else if (resultInt == 1)
                {
                    TempData["SuccessMessage"] = "Sipariþiniz Ýptal Edildi.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Not Recieved");
                }
                else if (resultInt == 2)
                {
                    TempData["SuccessMessage"] = "Sipariþ Ýptal Talebiniz Alýndý.\nÜrünler Tarafýmýza Ulaþtýðýnda Ücret Ýadesi Saðlanacaktýr.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Recieved");
                }
                else if (resultInt == 3)
                {
                    TempData["DangerMessage"] = "Ödeme iade edilirken hata ile karþýlaþýldý. \n" +
                                                 "Lütfen Ýletiþim Kutusundan Bizimle Ýletiþime Geçiniz.";
                }

                else if (resultInt == 4)
                {
                    TempData["SuccessMessage"] = "Sipariþiniz Ýptal Edildi.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Not Recieved");
                }

                else if (resultInt == 5)
                {
                    TempData["SuccessMessage"] = "Sipariþ Ýptal Talebiniz Alýndý.\nÜrünler Tarafýmýza Ulaþtýðýnda Ücret Ýadesi Saðlanacaktýr.";
                    await _emailService.SendOrderCancelledEmailAsync(order, email, "Products Recieved");
                }
                return Redirect("/Identity/Account/Manage/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                TempData["DangerMessage"] = "Sipariþ Ýptal Edilirken Hata ile Karþýlaþýldý";
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
                    TempData["DangerMessage"] = "Kullanýcý Bulunamadý.";
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
                    TempData["DangerMessage"] = "Sipariþ Ýptal Edilirken Hata ile Karþýlaþýldý. \n" +
                                                 "Lütfen Ýletiþim Kutusundan Bizimle Ýletiþime Geçiniz.";
                }
                else if (resultInt == 0)
                {
                    TempData["DangerMessage"] = "Ýlgili Sipariþ Sistemde Bulunamadý. \n" +
                                                 "Lütfen Ýletiþim Kutusundan Bizimle Ýletiþime Geçiniz.";
                }
                else if (resultInt == 1)
                {
                    TempData["SuccessMessage"] = "Sipariþiniz Ýptal Edildi.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order,item, email, "Products Not Recieved");
                }
                else if (resultInt == 2)
                {
                    TempData["SuccessMessage"] = "24 saat içerisinde yapýlan ödemeler sadece tamamen iptal edilebilir.\n" +
                                                "Sipariþi iptal edebilir ya da ürün bazlý ürün iptali için 24 saatin dolmasýný bekleyebilirsiniz.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order,item, email, "Products Not Recieved");
                }
                else if (resultInt == 3)
                {
                    TempData["DangerMessage"] = "Ürün tesliminden 15 gün sonra ürün iadesi kabul edilememektedir. \n" +
                                                 "Lütfen Ýletiþim Kutusundan Bizimle Ýletiþime Geçiniz.";
                }

                else if (resultInt == 4)
                {
                    TempData["SuccessMessage"] = "Sipariþiniz Ýptal Edildi.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order, item, email, "Products Not Recieved");
                }

                else if (resultInt == 5)
                {
                    TempData["SuccessMessage"] = "Sipariþ Ýptal Talebiniz Alýndý.\nÜrünler Tarafýmýza Ulaþtýðýnda Ücret Ýadesi Saðlanacaktýr.";
                    await _emailService.SendOrderItemCancelledEmailAsync(order, item, email, "Products Recieved");
                }
                return Redirect("/Identity/Account/Manage/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                TempData["DangerMessage"] = "Sipariþ Ýptal Edilirken Hata ile Karþýlaþýldý";
                return Redirect("/Identity/Account/Manage/Index");
            }
        }
    }
}
