// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Web.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<Author> _userManager;
        private readonly SignInManager<Author> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly CheepDBContext _context;
        private readonly IAuthorRepository _authorRepository;


        public DeletePersonalDataModel(
            UserManager<Author> userManager,
            SignInManager<Author> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            CheepDBContext context,
            IAuthorRepository authorRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _authorRepository = authorRepository;
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
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            var authorName = User.FindFirst("Name")?.Value ?? "User";

            var author = await _authorRepository.FindAuthorWithName(authorName);

            if (author != null)
            {
                // Reload the author from the DbContext with relational collections to manipulate navigation properties
                var authorEntry = await _context.Authors
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
                        _context.Cheeps.Remove(cheep);
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
                    _context.Authors.Remove(authorEntry);
                    await _context.SaveChangesAsync();
                }
            }

            await _signInManager.SignOutAsync();

            return Redirect("~/");
        }
    }
}
