// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class PublicModel : PageModel
{
    private readonly IAuthorRepository authorRepository;
    private readonly ICheepRepository cheepRepository;
    private readonly SignInManager<Author> signInManager;

    public PublicModel(ICheepRepository cheepRepository, IAuthorRepository authorRepository, SignInManager<Author> signInManager)
    {
        this.cheepRepository = cheepRepository;
        this.authorRepository = authorRepository;
        this.signInManager = signInManager;
    }

    public int PageSize { get; } = 32;

    public int PageNumber { get; set; }

    public List<CheepDTO> Cheeps { get; set; } = new List<CheepDTO>();

    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheep cannot be more than 160 characters.")]
    public string? Text { get; set; }

    public List<Author> Authors { get; set; } = new List<Author>();

    public List<Cheep> LikedCheeps { get; set; } = new List<Cheep>();

    public List<Author> FollowedAuthors { get; set; } = new List<Author>();

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
            && !await this.authorRepository.FindIfAuthorExistsWithEmail(this.User.Identity.Name))
        {
            await this.signInManager.SignOutAsync();
            var baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
            return this.Redirect($"{baseUrl}/");
        }

        // default to page number 1 if no page is specified
        var pageQuery = this.Request.Query["page"];
        this.PageNumber = int.TryParse(pageQuery, out int page) ? page : 1;

        this.Cheeps = await this.cheepRepository.GetCheeps(this.PageNumber, this.PageSize);

        if (this.User.Identity?.IsAuthenticated == true)
        {
            var authorEmail = this.User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(authorEmail))
            {
                var loggedInAuthor = await this.authorRepository.FindAuthorWithEmail(authorEmail);
                this.FollowedAuthors = await this.authorRepository.GetFollowing(loggedInAuthor.Id);
            }
        }

        return this.Page();
    }

    /// <summary>
    /// Handles POST requests to publish a new cheep by the logged-in user.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after saving the cheep.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPost()
    {
        var authorName = this.User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.authorRepository.FindAuthorWithEmail(authorName);
        var cheep = new Cheep
        {
            AuthorId = author.Id,
            Text = this.Text,
            TimeStamp = DateTime.UtcNow,
            Author = author,
        };

        if (cheep.Text != null)
        {
            await this.cheepRepository.SaveCheep(cheep, author);
        }

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to follow another user.
    /// </summary>
    /// <param name="followAuthorName">The name of the author to follow.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostFollow(string followAuthorName)
    {
        // Finds the author thats logged in
        var authorName = this.User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.authorRepository.FindAuthorWithEmail(authorName);

        // Finds the author that the logged in author wants to follow
        var followAuthor = await this.authorRepository.FindAuthorWithName(followAuthorName);

        await this.authorRepository.FollowUserAsync(author.Id, followAuthor.Id);

        // updates the current author's list of followed authors
        this.FollowedAuthors = await this.authorRepository.GetFollowing(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to unfollow another user.
    /// </summary>
    /// <param name="followAuthorName">The name of the author to unfollow.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostUnfollow(string followAuthorName)
    {
        // Finds the author thats logged in
        var authorName = this.User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.authorRepository.FindAuthorWithEmail(authorName);

        // Finds the author that the logged in author wants to follow
        var followAuthor = await this.authorRepository.FindAuthorWithName(followAuthorName);

        await this.authorRepository.UnFollowUserAsync(author.Id, followAuthor.Id);

        // updates the current author's list of followed authors
        this.FollowedAuthors = await this.authorRepository.GetFollowing(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to like a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The author of the cheep to like.</param>
    /// <param name="text">The text of the cheep to like.</param>
    /// <param name="timeStamp">The timestamp of the cheep to like.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostLike(string cheepAuthorName, string text, string timeStamp)
    {
        // Find the author that's logged in
        var authorName = this.User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.authorRepository.FindAuthorWithName(authorName);
        var cheep = await this.cheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }

        // Adds the cheep to the author's list of liked cheeps
        await this.cheepRepository.LikeCheep(cheep, author);

        this.LikedCheeps = await this.authorRepository.GetLikedCheeps(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to unlike a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The author of the cheep to unlike.</param>
    /// <param name="text">The text of the cheep to unlike.</param>
    /// <param name="timeStamp">The timestamp of the cheep to unlike.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostUnLike(string cheepAuthorName, string text, string timeStamp)
    {
        // Find the author that's logged in
        var authorName = this.User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.authorRepository.FindAuthorWithName(authorName);
        var cheep = await this.cheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }

        await this.cheepRepository.UnLikeCheep(cheep, author);

        this.LikedCheeps = await this.authorRepository.GetLikedCheeps(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Determines whether the logged-in user has liked a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The author of the cheep.</param>
    /// <param name="text">The text of the cheep.</param>
    /// <param name="timeStamp">The timestamp of the cheep.</param>
    /// <returns>A <see cref="bool"/> indicating whether the cheep is liked by the user.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<bool> DoesUserLikeCheep(string cheepAuthorName, string text, string timeStamp)
    {
        var authorName = this.User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.authorRepository.FindAuthorWithName(authorName);
        var cheep = await this.cheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            string message = $"Cheep could not be found. Text: {text}, TimeStamp: {timeStamp}, Author: {cheepAuthorName}";
            throw new ArgumentException(message);
        }

        return await this.cheepRepository.DoesUserLikeCheep(cheep, author);
    }
}
