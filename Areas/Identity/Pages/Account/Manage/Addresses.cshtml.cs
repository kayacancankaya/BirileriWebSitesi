using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account.Manage
{
    public class AddressesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IOrderService _orderService;
        public AddressesModel(ApplicationDbContext context, UserManager<IdentityUser> userManager,
                                IOrderService orderService)
        {
            _context = context;
            _userManager = userManager;
            _orderService = orderService;
        }

        public List<Address> Addresses { get; set; } = new();
        [BindProperty]
        public Address FormAddress { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = ManageNavPages.Addresses;
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return Page();
        }
        public async Task<IActionResult> OnGetAddressInfoAsync(int id)
        {
            FormAddress = await _context.Addresses.Where(a=>a.Id == id)
                                                  .FirstOrDefaultAsync();
            if (FormAddress == null)
            {
                return Content("Adres bulunamadı.");
            }

            return Partial("_PartialSelectedAddress", FormAddress);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateAddressAsync()
        {
            try
            {

                if (!ModelState.IsValid)
                {
                    // collect all error messages
                    var errors = ModelState
                        .Where(kvp => kvp.Value.Errors.Count > 0)
                        .SelectMany(kvp => kvp.Value.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    // 400 → will hit $.ajax error callback
                    return BadRequest(new { success = false, errors });
                }
                //if it is shipping address
                //check first name and last name
                if(!FormAddress.IsBilling)
                {
                    if(string.IsNullOrEmpty(FormAddress.FirstName))
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            message = "İsim bilgisi eksik."
                        });
                    }
                    if(string.IsNullOrEmpty(FormAddress.LastName))
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            message = "Soy İsim bilgisi eksik."
                        });
                    }
                }
                //if it is billing address
                //if it is corporate check corporte info
                //if it is not corp check name info
                else
                {
                    if (FormAddress.IsCorporate)
                    {
                        if (string.IsNullOrEmpty(FormAddress.CorporateName))
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                message = "Firma bilgisi eksik."
                            });
                        }
                        if (string.IsNullOrEmpty(FormAddress.VATstate))
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                message = "Vergi Dairesi Eksik."
                            });
                        }
                        if (string.IsNullOrEmpty(FormAddress.VATnumber))
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                message = "Vergi Numarası Eksik."
                            });
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(FormAddress.FirstName))
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                message = "İsim bilgisi eksik."
                            });
                        }
                        if (string.IsNullOrEmpty(FormAddress.LastName))
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                message = "Soy İsim bilgisi eksik."
                            });
                        }

                    }
                }
                //update address db
                bool result = await _orderService.UpdateAddressAsync(FormAddress);
                if (!result)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Adres bilgisi güncellenemedi."
                    });
                }

                return new JsonResult(new
                {
                    success = true,
                    message = "Adres bilgisi güncellendi."
                });

            }
            catch (Exception)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Adres bilgisi güncellenirken sistemsel hata ile karşılaşıldı."
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAddressAsync()
        {
            try
            {
                if(FormAddress.Id <= 0)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Adres bilgisi bulunamadı."
                    });
                }

                bool result = await _orderService.DeleteAddressAsync(FormAddress.Id);
                if(!result)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Adres bilgisi silinemedi."
                    });
                }

                TempData["SuccessMessage"] = "Adres Bilgisi Silindi";
                
                    return new JsonResult(new
                    {
                        success = true,
                        message = "Adres bilgisi silindi."
                    });
                

            }
            catch (Exception)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Adres bilgisi silinirken sistemsel hata ile karşılaşıldı."
                });

            }
        }
    }
}
