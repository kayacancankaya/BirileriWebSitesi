// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Antiforgery;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Data;
using Microsoft.EntityFrameworkCore;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Controllers;
using BirileriWebSitesi.Helpers;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAntiforgery _antiforgery;
        private readonly IBasketService _basketService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserAuditService _userAuditService;
        public LoginModel(SignInManager<IdentityUser> signInManager,  UserManager<IdentityUser> userManager,
                             IAntiforgery antiforgery,
                             IBasketService basketService,
                             ApplicationDbContext dbContext, 
                             IUserAuditService userAuditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _antiforgery = antiforgery;
            _basketService = basketService;
            _dbContext = dbContext;
            _userAuditService = userAuditService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Email Alanı Zorunlu")]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Şifre Alanı Zorunlu")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Beni Hatırla?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = Input.RememberMe,
                            ExpiresUtc = Input.RememberMe
                                ? DateTimeOffset.UtcNow.AddDays(14)  // 14 days if Remember Me is checked
                                : DateTimeOffset.UtcNow.AddMinutes(30) // Default 5-minute expiration
                        };

                        // Explicitly re-sign in with custom expiration settings
                        await _signInManager.SignInAsync(user, authProperties);
                        // Update the last login date in the database
                        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                        bool isProduction = environment == "Production";
                        if (isProduction)
                        {
                            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                            await _userAuditService.CreateUserAudit(user.Id, DateTime.UtcNow, ip);
                        }
                               
                    }
                    _antiforgery.GetAndStoreTokens(HttpContext);
                    string cart = Request.Cookies["MyCart"];
                    if (!string.IsNullOrEmpty(cart))
                    {
                        await _basketService.TransferBasketAsync(cart, user.Id);
                        HttpContext.Response.Cookies.Delete("MyCart");
                    }
                   
                    string inquiry = Request.Cookies["MyInquiry"];
                    if (!string.IsNullOrEmpty(inquiry))
                    {
                        await _basketService.TransferInquiryBasketAsync(inquiry, user.Id);
                        HttpContext.Response.Cookies.Delete("MyInquiry");
                    }
                    return RedirectToPage("./Manage/Index");

                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Başarısız Oturum Açma İsteği.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
