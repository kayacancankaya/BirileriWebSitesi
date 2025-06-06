﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account
{
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ConfirmEmailChangeModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                TempData["DangerMessage"] = "Kullanıcı bilgileri bulunamadı.";
               return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });

            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["DangerMessage"] = "Kullanıcı bilgileri bulunamadı.";
                return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                 var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                StatusMessage = $"Mail Değiştirilemedi: {errors}";
                
                return Page();
            }

            // In our UI email and user name are one and the same, so when we update the email
            // we need to update the user name.
            var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
            if (!setUserNameResult.Succeeded)
            {
                 var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                StatusMessage = $"Kullanıcı adı değiştirilemedi.: {errors}";
                
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Mail değişiklini onayladığınız için teşekkür ederiz.";
            return Page();
        }
    }
}
