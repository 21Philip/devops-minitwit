// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages.Shared;

public class TimelineModel : PageModel
{
    public TimelineModel(ICheepRepository cheepRepository, IAuthorRepository authorRepository)
    {
        this.CheepRepository = cheepRepository;
        this.AuthorRepository = authorRepository;
    }

    public List<CheepDTO> Cheeps { get; set; } = new List<CheepDTO>();

    public int PageSize { get; } = 32;

    public int PageNumber { get; set; } = 1;

    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheep cannot be more than 160 characters.")]
    public string? Text { get; set; }

    public List<Author> FollowedAuthors { get; set; } = new List<Author>();

    public List<Cheep> LikedCheeps { get; set; } = new List<Cheep>();

    protected IAuthorRepository AuthorRepository { get; }

    protected ICheepRepository CheepRepository { get; }

    /// <summary>
    /// Handles POST requests to create a new cheep by the logged-in user.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after saving the cheep.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPost()
    {
        var authorName = this.User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        Author author = await this.AuthorRepository.FindAuthorWithName(authorName);
        var cheep = new Cheep
        {
            AuthorId = author.Id,
            Text = this.Text,
            TimeStamp = DateTime.UtcNow,
            Author = author,
        };

        if (cheep.Text != null)
        {
            await this.CheepRepository.SaveCheep(cheep, author);
        }

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to follow another author.
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

        var author = await this.AuthorRepository.FindAuthorWithEmail(authorName);

        // Finds the author that the logged in author wants to follow
        var followAuthor = await this.AuthorRepository.FindAuthorWithName(followAuthorName);

        await this.AuthorRepository.FollowUserAsync(author.Id, followAuthor.Id);

        // updates the current author's list of followed authors
        this.FollowedAuthors = await this.AuthorRepository.GetFollowing(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to unfollow an author they follow.
    /// </summary>
    /// <param name="followAuthorName">The name of the author to unfollow.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostUnfollow(string followAuthorName)
    {
        // Finds the author thats logged in
        var authorName = this.User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.AuthorRepository.FindAuthorWithName(authorName);

        // Finds the author that the logged in author wants to follow
        var followAuthor = await this.AuthorRepository.FindAuthorWithName(followAuthorName);

        await this.AuthorRepository.UnFollowUserAsync(author.Id, followAuthor.Id);

        // updates the current author's list of followed authors
        this.FollowedAuthors = await this.AuthorRepository.GetFollowing(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to like other authors cheeps.
    /// </summary>
    /// <param name="cheepAuthorName">The name of the author of the cheep.</param>
    /// <param name="text">The text, excluding author and timestamp, of the cheep.</param>
    /// <param name="timeStamp">The time of which the cheep was posted.</param>
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

        var author = await this.AuthorRepository.FindAuthorWithName(authorName);
        var cheep = await this.CheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }

        // Adds the cheep to the author's list of liked cheeps
        await this.CheepRepository.LikeCheep(cheep, author);

        this.LikedCheeps = await this.AuthorRepository.GetLikedCheeps(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to remove like, from already liked cheeps.
    /// </summary>
    /// <param name="cheepAuthorName">The name of the author of the cheep.</param>
    /// <param name="text">The text, excluding author and timestamp, of the cheep.</param>
    /// <param name="timeStamp">The time of which the cheep was posted.</param>
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

        var author = await this.AuthorRepository.FindAuthorWithName(authorName);
        var cheep = await this.CheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }

        await this.CheepRepository.UnLikeCheep(cheep, author);

        this.LikedCheeps = await this.AuthorRepository.GetLikedCheeps(author.Id);

        return this.RedirectToPage();
    }

    /// <summary>
    /// Checks if the logged-in user has liked a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The name of the author of the cheep.</param>
    /// <param name="text">The text, excluding author and timestamp, of the cheep.</param>
    /// <param name="timeStamp">The time of which the cheep was posted.</param>
    /// <returns>A <see cref="bool"/> returns true if the cheep has been liked by the logged-in user.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<bool> DoesUserLikeCheep(string cheepAuthorName, string text, string timeStamp)
    {
        var authorName = this.User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await this.AuthorRepository.FindAuthorWithName(authorName);
        var cheep = await this.CheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }

        return await this.CheepRepository.DoesUserLikeCheep(cheep, author);
    }
}