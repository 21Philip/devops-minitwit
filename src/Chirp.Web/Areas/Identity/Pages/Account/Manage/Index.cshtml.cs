// Copyright (c) devops-gruppe-connie. All rights reserved.

#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<Author> userManager;
        private readonly SignInManager<Author> signInManager;
        private readonly CheepDBContext context;
        private readonly ICheepRepository cheepRepository;

        public IndexModel(UserManager<Author> userManager, SignInManager<Author> signInManager, CheepDBContext context, ICheepRepository cheepRepository)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
            this.cheepRepository = cheepRepository;
        }

        public string Email { get; set; }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
            }

            await this.LoadAsync(user);
            return this.Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
            }

            if (!this.ModelState.IsValid)
            {
                await this.LoadAsync(user);
                return this.Page();
            }

            if (user.PhoneNumber != this.Input.PhoneNumber)
            {
                var existingClaim = (await this.userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "PhoneNumber");
                if (existingClaim != null)
                {
                    // Removes the claim if the claim exists
                    var removeResult = await this.userManager.RemoveClaimAsync(user, existingClaim);
                    if (!removeResult.Succeeded)
                    {
                        this.StatusMessage = "Unexpected error when trying to remove existing phone number claim.";
                        return this.RedirectToPage();
                    }
                }

                // Creates a new claim with the new username.
                var newClaim = new Claim("PhoneNumber", this.Input.PhoneNumber);

                // Adds the claim to database
                var addClaimResult = await this.userManager.AddClaimAsync(user, newClaim);
                if (!addClaimResult.Succeeded)
                {
                    this.StatusMessage = "Unexpected error when trying to add new phone number claim.";
                    return this.RedirectToPage();
                }

                // This updates the users (authors) name, which also makes sure that the cheeps have the NewUserName
                user.PhoneNumber = this.Input.PhoneNumber;
                var updateResult = await this.userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        this.ModelState.AddModelError(string.Empty, error.Description);
                    }

                    this.StatusMessage = "Unexpected error when trying to update phone number.";
                    return this.RedirectToPage();
                }
            }

            if (user.Name != this.Input.NewUserName)
            {
                // Gets the existing claim (Which is made in Register when a new user is created).
                var existingClaim = (await this.userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "Name");
                if (existingClaim != null)
                {
                    // Removes the claim if the claim exists
                    var removeResult = await this.userManager.RemoveClaimAsync(user, existingClaim);
                    if (!removeResult.Succeeded)
                    {
                        this.StatusMessage = "Unexpected error when trying to remove existing name claim.";
                        return this.RedirectToPage();
                    }
                }

                // Creates a new claim with the new username.
                var newClaim = new Claim("Name", this.Input.NewUserName);

                // Adds the claim to database
                var addClaimResult = await this.userManager.AddClaimAsync(user, newClaim);
                if (!addClaimResult.Succeeded)
                {
                    this.StatusMessage = "Unexpected error when trying to add new name claim.";
                    return this.RedirectToPage();
                }

                // This updates the users (authors) name, which also makes sure that the cheeps have the NewUserName
                user.Name = this.Input.NewUserName;
                var updateResult = await this.userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        this.ModelState.AddModelError(string.Empty, error.Description);
                    }

                    this.StatusMessage = "Unexpected error when trying to update name.";
                    return this.RedirectToPage();
                }
            }

            await this.signInManager.RefreshSignInAsync(user);
            this.StatusMessage = "Your profile has been updated";
            return this.RedirectToPage();
        }

        private async Task LoadAsync(Author user)
        {
            this.Email = await this.userManager.GetEmailAsync(user); // Retrieve email
            var phoneNumber = await this.userManager.GetPhoneNumberAsync(user);
            this.Username = user.Name; // Set current username

            this.Input = new InputModel
            {
                NewUserName = this.Username, // Pre-fill with the current username
                PhoneNumber = phoneNumber,
            };
        }

        public class InputModel
        {
            [Display(Name = "NewUserName")]
            public string NewUserName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }
    }
}
