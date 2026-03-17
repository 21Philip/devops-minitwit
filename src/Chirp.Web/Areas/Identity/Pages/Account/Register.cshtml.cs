// Copyright (c) devops-gruppe-connie. All rights reserved.

#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<Author> signInManager;
        private readonly UserManager<Author> userManager;
        private readonly IUserStore<Author> userStore;
        private readonly IUserEmailStore<Author> emailStore;
        private readonly ILogger<RegisterModel> logger;
        private readonly ICheepRepository cheepRepository;

        public RegisterModel(
            UserManager<Author> userManager,
            IUserStore<Author> userStore,
            SignInManager<Author> signInManager,
            ILogger<RegisterModel> logger,
            ICheepRepository cheepRepository)
        {
            this.userManager = userManager;
            this.userStore = userStore;
            this.emailStore = this.GetEmailStore();
            this.signInManager = signInManager;
            this.logger = logger;
            this.cheepRepository = cheepRepository;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            this.ReturnUrl = returnUrl ?? this.Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= this.Url.Content("~/");

            if (!this.ModelState.IsValid)
            {
                return this.Page();
            }

            // Check email uniqueness
            var existingByEmail = await this.userManager.FindByEmailAsync(this.Input.Email);
            if (existingByEmail != null)
            {
                this.ModelState.AddModelError("Input.Email", $"Email '{this.Input.Email}' is already taken.");
                return this.Page();
            }

            var user = this.CreateUser();

            // Due to backwards compatability issues: UserName == Email
            await this.userStore.SetUserNameAsync(user, this.Input.Email, CancellationToken.None);
            await this.emailStore.SetEmailAsync(user, this.Input.Email, CancellationToken.None);

            user.Name = this.Input.Name;
            user.Cheeps = new List<Cheep>();

            var result = await this.userManager.CreateAsync(user, this.Input.Password);

            if (result.Succeeded)
            {
                var claim = new Claim("Name", this.Input.Name);
                await this.userManager.AddClaimAsync(user, claim);
                this.logger.LogInformation("User created a new account with password.");

                await this.signInManager.SignInAsync(user, isPersistent: false);
                return this.LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                this.ModelState.AddModelError(string.Empty, error.Description);
            }

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
                    $"Ensure it has a parameterless constructor.");
            }
        }

        private IUserEmailStore<Author> GetEmailStore()
        {
            if (!this.userManager.SupportsUserEmail)
            {
                throw new System.NotSupportedException("The UI requires a user store with email support.");
            }

            return (IUserEmailStore<Author>)this.userStore;
        }

        public class InputModel
        {
            [Required]
            [Display(Name = "Name")]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}
