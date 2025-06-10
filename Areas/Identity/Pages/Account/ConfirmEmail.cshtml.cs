// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using BirileriWebSitesi.Interfaces;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IUserAuditService _userAudit;
        
        public ConfirmEmailModel(UserManager<IdentityUser> userManager,
                                 ILogger<ConfirmEmailModel> logger,
                                 SignInManager<IdentityUser> signInManager,
                                 IUserAuditService userAudit)
        {
            _userManager = userManager;
            _logger = logger;
            _signInManager = signInManager;
            _userAudit = userAudit;
        }
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Kullanıcı listelenemedi '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "Mailinizi doğruladığınız için teşekkür ederiz." : "Mail doğrulanırken hata ile karşılaşıldı.";
            if(result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _userAudit.CreateUserAudit(user.Id, DateTime.UtcNow, ip);
            }
               
            return Page();
        }
    }
}
