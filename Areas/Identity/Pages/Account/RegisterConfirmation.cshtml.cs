// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BirileriWebSitesi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BirileriWebSitesi.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterConfirmationModel> _logger;

        public RegisterConfirmationModel(UserManager<IdentityUser> userManager, IEmailService service,
                                        ILogger<RegisterConfirmationModel> logger)
        {
            _userManager = userManager;
            _emailService = service;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool DisplayConfirmAccountLink { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string EmailConfirmationUrl { get; set; }
       
        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            _logger.LogWarning("Register confirmation on get started.");
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Email bulunamadı '{email}'.");
            }

            Email = email;
            DisplayConfirmAccountLink = true;
            if (DisplayConfirmAccountLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = ./Identity/Account/RegisterConfirmation.cshtml },
                    protocol: Request.Scheme);
            }
            // Email content
            var subject = "Hesabınızı Onaylayın";
            var htmlMessage = $"Lütfen hesabınızı onaylamak için  <a href='{HtmlEncoder.Default.Encode(code)}'>Buraya Tıklayınız...</a>.";

            // Send the email
            bool result = await _emailService.SendEmailAsync(email, subject, htmlMessage);
            
            if(result == false)
            {
                TempData["ErrorMessage"] = "Doğrulama maili gönderilemedi. Lütfen daha sonra tekrar deneyiniz.";
                return RedirectToPage("/Account/Register");
            }

            _logger.LogWarning("Register confirmation on get returns page.");
            return Page();
        }
    }
}
