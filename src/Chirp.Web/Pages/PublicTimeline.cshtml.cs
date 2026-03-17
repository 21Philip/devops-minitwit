// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Web.Pages.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Chirp.Web.Pages;

public class PublicTimelineModel : TimelineModel
{
    private readonly SignInManager<Author> signInManager;

    public PublicTimelineModel(ICheepRepository cheepRepository, IAuthorRepository authorRepository, SignInManager<Author> signInManager)
        : base(cheepRepository, authorRepository)
    {
        this.signInManager = signInManager;
    }

    /// <summary>
    /// Handles GET requests to display the public timeline and user-specific data if authenticated.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> indicating the result of the operation.</returns>
    /// <remarks>
    /// Authenticated users are validated against the database. If not found, they are signed out and redirected.
    /// </remarks>
    public async Task<ActionResult> OnGet()
    {
        // check if logged-in user exists in database, otherwise log out and redirect to public timeline
        if (this.signInManager.IsSignedIn(this.User)
            && !string.IsNullOrEmpty(this.User.Identity?.Name)
            && !await this.AuthorRepository.FindIfAuthorExistsWithEmail(this.User.Identity.Name))
        {
            await this.signInManager.SignOutAsync();
            var baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
            return this.Redirect($"{baseUrl}/");
        }

        // default to page number 1 if no page is specified
        var pageQuery = this.Request.Query["page"];
        this.PageNumber = int.TryParse(pageQuery, out int page) ? page : 1;

        this.Cheeps = await this.CheepRepository.GetCheeps(this.PageNumber, this.PageSize);

        if (this.User.Identity?.IsAuthenticated == true)
        {
            var authorEmail = this.User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(authorEmail))
            {
                var loggedInAuthor = await this.AuthorRepository.FindAuthorWithEmail(authorEmail);
                this.FollowedAuthors = await this.AuthorRepository.GetFollowing(loggedInAuthor.Id);
            }
        }

        return this.Page();
    }
}
