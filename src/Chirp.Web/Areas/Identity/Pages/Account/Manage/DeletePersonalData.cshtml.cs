// Copyright (c) devops-gruppe-connie. All rights reserved.

#nullable disable

using System.ComponentModel.DataAnnotations;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<Author> userManager;
        private readonly SignInManager<Author> signInManager;
        private readonly IAuthorRepository authorRepository;

        public DeletePersonalDataModel(
            UserManager<Author> userManager,
            SignInManager<Author> signInManager,
            IAuthorRepository authorRepository)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.authorRepository = authorRepository;
        }

        /// <summary>
        ///     Gets or sets this API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
            }

            this.RequirePassword = await this.userManager.HasPasswordAsync(user);
            return this.Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
            }

            this.RequirePassword = await this.userManager.HasPasswordAsync(user);
            bool validPassword = await this.userManager.CheckPasswordAsync(user, this.Input.Password);

            if (this.RequirePassword && !validPassword)
            {
                this.ModelState.AddModelError(string.Empty, "Incorrect password.");
                return this.Page();
            }

            var authorName = this.User.FindFirst("Name")?.Value ?? "User";

            var author = await this.authorRepository.FindAuthorWithName(authorName);

            if (author != null)
            {
                await this.authorRepository.DeleteAuthor(author);
            }

            await this.signInManager.SignOutAsync();

            return this.Redirect("~/");
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     Gets or sets this API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }
    }
}
