﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace BirileriWebSitesi.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAntiforgery _antiforgery;
        private readonly IBasketService _basketService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserAuditService _userAuditService;
        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger, UserManager<IdentityUser> userManager,
                             IAntiforgery antiforgery,
                             IBasketService basketService,
                             ApplicationDbContext dbContext, 
                             IUserAuditService userAuditService)
        {
            _signInManager = signInManager;
            _logger = logger;
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
                        var audit = await _dbContext.UserAudits.FirstOrDefaultAsync(a => a.UserId == user.Id);
                        if (audit != null)
                        {
                            audit.LastLoginDate = DateTime.UtcNow;
                            await _dbContext.SaveChangesAsync();

                        }
                        else
                        {
                            _dbContext.UserAudits.Add(new UserAudit
                            {
                                UserId = user.Id,
                                RegistrationDate = DateTime.UtcNow,
                                LastLoginDate = DateTime.UtcNow
                            });
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    _antiforgery.GetAndStoreTokens(HttpContext);
                    _logger.LogInformation("Kullanıcı Kayıt Oldu.");
                    string cart = Request.Cookies["MyCart"];
                    if (!string.IsNullOrEmpty(cart))
                    {
                        await _basketService.TransferBasketAsync(cart, user.Id);
                        HttpContext.Response.Cookies.Delete("MyCart");
                    }
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Kullanıcı Hesabı Kilitli.");
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
