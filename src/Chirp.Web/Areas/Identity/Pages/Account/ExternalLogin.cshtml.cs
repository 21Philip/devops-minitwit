// Copyright (c) devops-gruppe-connie. All rights reserved.

#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Chirp.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<Author> signInManager;
        private readonly UserManager<Author> userManager;
        private readonly IUserStore<Author> userStore;
        private readonly IUserEmailStore<Author> emailStore;
        private readonly IEmailSender emailSender;
        private readonly ILogger<ExternalLoginModel> logger;

        public ExternalLoginModel(
            SignInManager<Author> signInManager,
            UserManager<Author> userManager,
            IUserStore<Author> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.userStore = userStore;
            this.emailStore = this.GetEmailStore();
            this.logger = logger;
            this.emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        // This will redirect users to the login page
        public IActionResult OnGet() => this.RedirectToPage("./Login");

        // External login provider challenge
        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = this.Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = this.signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // Callback method to handle the response from the external provider
        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= this.Url.Content("~/");

            if (remoteError != null)
            {
                this.ErrorMessage = $"Error from external provider: {remoteError}";
                return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await this.signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                this.ErrorMessage = "Error loading external login information.";
                return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                this.ErrorMessage = "Email address not provided by external provider.";
                return this.RedirectToPage("./ExternalLoginConfirmation", new { returnUrl });
            }

            var user = await this.userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // User doesn't exist; create a new user
                user = this.CreateUser();
                await this.userStore.SetUserNameAsync(user, email, CancellationToken.None);
                await this.emailStore.SetEmailAsync(user, email, CancellationToken.None);

                // Set user properties from external provider
                user.Name = info.Principal.Identity.Name ?? "Unknown";
                user.Id = await this.userManager.Users.CountAsync() + 1;

                var createUserResult = await this.userManager.CreateAsync(user);
                if (createUserResult.Succeeded)
                {
                    await this.userManager.AddClaimAsync(user, new Claim("Name", user.Name));
                    var addLoginResult = await this.userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                    {
                        this.ErrorMessage = "Failed to add external login for new user.";
                        return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }
                }
                else
                {
                    this.ErrorMessage = "Failed to create user.";
                    foreach (var error in createUserResult.Errors)
                    {
                        this.ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
            }

            // Sign in the user
            await this.signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            this.logger.LogInformation("User signed in with {Name} provider.", info.LoginProvider);

            // Redirect to the login page after successful registration or sign-in
            return this.Redirect("~/");
        }

        // Final confirmation after external login
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= this.Url.Content("~/");

            var info = await this.signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                this.ErrorMessage = "Error loading external login information during confirmation.";
                return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (this.ModelState.IsValid)
            {
                var user = this.CreateUser();

                // Set email if it's retrieved from the external provider
                if (string.IsNullOrEmpty(this.Input.Email))
                {
                    this.Input.Email = info.Principal.FindFirstValue(ClaimTypes.Email); // Retrieve email from the external provider
                }

                await this.userStore.SetUserNameAsync(user, this.Input.Email, CancellationToken.None);
                await this.emailStore.SetEmailAsync(user, this.Input.Email, CancellationToken.None);

                // Set Name from external provider if available
                user.Name = info.Principal.Identity.Name ?? "Unknown";
                user.Id = await this.userManager.Users.CountAsync() + 1;

                var result = await this.userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    var claim = new Claim("Name", user.Name);
                    await this.userManager.AddClaimAsync(user, claim);

                    result = await this.userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        this.logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        // Sign in the user
                        await this.signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                        // Redirect to the login page after successful registration
                        return this.RedirectToPage("./Login");
                    }
                }

                foreach (var error in result.Errors)
                {
                    this.ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            this.ProviderDisplayName = info.ProviderDisplayName;
            this.ReturnUrl = returnUrl;
            return this.Page();
        }

        private Author CreateUser()
        {
            try
            {
                return Activator.CreateInstance<Author>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(Author)}'. " +
                    $"Ensure that '{nameof(Author)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<Author> GetEmailStore()
        {
            if (!this.userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<Author>)this.userStore;
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
    }
}
