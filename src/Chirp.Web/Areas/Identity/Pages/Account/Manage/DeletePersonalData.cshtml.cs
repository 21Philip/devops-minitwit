// Copyright (c) devops-gruppe-connie. All rights reserved.

#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<Author> userManager;
        private readonly SignInManager<Author> signInManager;
        private readonly ILogger<DeletePersonalDataModel> logger;
        private readonly CheepDBContext context;
        private readonly IAuthorRepository authorRepository;

        public DeletePersonalDataModel(
            UserManager<Author> userManager,
            SignInManager<Author> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            CheepDBContext context,
            IAuthorRepository authorRepository)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.context = context;
            this.authorRepository = authorRepository;
        }

        /// <summary>
        ///     Gets or sets this API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

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
            if (this.RequirePassword)
            {
                if (!await this.userManager.CheckPasswordAsync(user, this.Input.Password))
                {
                    this.ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return this.Page();
                }
            }

            var authorName = this.User.FindFirst("Name")?.Value ?? "User";

            var author = await this.authorRepository.FindAuthorWithName(authorName);

            if (author != null)
            {
                // Reload the author from the DbContext with relational collections to manipulate navigation properties
                var authorEntry = await this.context.Authors
                    .Include(a => a.Cheeps)
                    .Include(a => a.Followers)
                    .Include(a => a.FollowedAuthors)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(a => a.Id == author.Id);

                if (authorEntry != null)
                {
                    // Remove cheeps
                    var cheepsToDelete = authorEntry.Cheeps?.ToList() ?? new System.Collections.Generic.List<Chirp.Core.Cheep>();
                    foreach (var cheep in cheepsToDelete)
                    {
                        this.context.Cheeps.Remove(cheep);
                    }

                    // Clear follower relationships (authors who follow this author)
                    if (authorEntry.Followers != null)
                    {
                        foreach (var follower in authorEntry.Followers.ToList())
                        {
                            authorEntry.Followers.Remove(follower);
                        }
                    }

                    // Clear followed relationships (authors this author follows)
                    if (authorEntry.FollowedAuthors != null)
                    {
                        foreach (var followed in authorEntry.FollowedAuthors.ToList())
                        {
                            authorEntry.FollowedAuthors.Remove(followed);
                        }
                    }

                    // Now safe to remove the author
                    this.context.Authors.Remove(authorEntry);
                    await this.context.SaveChangesAsync();
                }
            }

            await this.signInManager.SignOutAsync();

            return this.Redirect("~/");
        }
    }
}
