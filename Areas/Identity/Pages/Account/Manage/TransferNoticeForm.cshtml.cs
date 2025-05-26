using BirileriWebSitesi.Data;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
public class TransferNoticeFormModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IEmailService _emailService;

    public TransferNoticeFormModel(IOrderService orderService, IEmailService emailService)
    {
        _orderService = orderService;
        _emailService = emailService;
    }

    [BindProperty]
    public int? SelectedOrderId { get; set; }

    [BindProperty]
    [MaxLength(500)]
    public string Note { get; set; }

    public List<Order> Orders { get; set; }

    public IEnumerable<SelectListItem> OrderSelectList =>
        Orders?.Select(o => new SelectListItem
        {
            Value = o.Id.ToString(),
            Text = $"#{o.Id} - {o.OrderDate:dd.MM.yyyy}"
        }) ?? Enumerable.Empty<SelectListItem>();

    public async Task OnGetAsync()
    {
        Orders = await _orderService.GetBankTransferOrdersForUserAsync(User);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || SelectedOrderId == null)
        {
            Orders = await _orderService.GetBankTransferOrdersForUserAsync(User);
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
