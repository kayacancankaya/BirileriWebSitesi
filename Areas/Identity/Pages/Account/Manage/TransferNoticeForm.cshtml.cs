using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
public class TransferNoticeFormModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IEmailService _emailService;
     private readonly UserManager<IdentityUser> _userManager;
       
    public TransferNoticeFormModel(IOrderService orderService, IEmailService emailService,
                                    UserManager<IdentityUser> userManager)
    {
        _orderService = orderService;
        _emailService = emailService;
        _userManager = userManager;
    }

    [BindProperty]
    public int? SelectedOrderId { get; set; }

    [BindProperty]
    [MaxLength(500)]
    public string Note { get; set; }

    [BindProperty]
    public Dictonary<int,string> OrderInfos { get; set; }
    [BindProperty]
    SelectList OrderSelectList { get; set; }
    public async Task OnGetAsync()
    {
        string userID = _userManager.GetUserId(User);
        OrderInfos = await _orderService.GetBankTransferOrdersForUserAsync(userID);
        OrderSelectList = new SelectList(OrderInfos, "Key", "Value");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || SelectedOrderId == null)
        {
            string userID = _userManager.GetUserId(User);
            OrderInfos = await _orderService.GetBankTransferOrdersForUserAsync(userID);
            OrderSelectList = new SelectList(OrderInfos, "Key", "Value");
            return Page();
        }

        var result = await _emailService.SendBankTransferNoticeEmailAsync(User.Identity.Name, SelectedOrderId.Value, Note);

        if (result)
        {
            TempData["Success"] = "Bildirim başarıyla gönderildi.";
        }
        else
        {
            TempData["Error"] = "Bildirim gönderilirken bir hata oluştu.";
        }

        return RedirectToPage(); // redirect to itself
    }
}
