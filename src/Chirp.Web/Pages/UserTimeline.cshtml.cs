// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

/// <summary>
/// This class handles the users interactions with the user's timeline page.
/// This includes posting and liking cheeps, as well as following/unfollowing author.
/// </summary>
public class UserTimelineModel : PageModel
{
    private readonly IAuthorRepository authorRepository;
    private readonly ICheepRepository cheepRepository;

    public UserTimelineModel(ICheepRepository cheepRepository, IAuthorRepository authorRepository)
    {
        this.cheepRepository = cheepRepository;
        this.authorRepository = authorRepository;
    }

    public List<CheepDTO> Cheeps { get; set; } = new List<CheepDTO>();

    public int PageSize { get; } = 32;

    public int PageNumber { get; set; } = 1;

    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheep cannot be more than 160 characters.")]
    public string? Text { get; set; }

    public List<Author> FollowedAuthors { get; set; } = new List<Author>();

    public List<Cheep> LikedCheeps { get; set; } = new List<Cheep>();

    /// <summary>
    /// Handles GET requests to display the user's timeline.
    /// Content of the page differentiate whether it's the logged-in user's timeline, or another user's.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> for rendering the page.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the logged-in user's email is missing or not authenticated.</exception>
    public async Task<ActionResult> OnGet()
    {
        // Gets the authorName from the currently LOGGED IN user
        var authorName = this.User.FindFirst("Name")?.Value ?? "User";

        // Gets the author name from the URL.
        var pageUser = this.HttpContext.GetRouteValue("author")?.ToString() ?? "DefaultUser";

        // This checks if the logged in user's USERNAME equals to the value from the UserTimeline URL
        if (authorName == pageUser)
        {
            var pageQuery = this.Request.Query["page"];
            if (!string.IsNullOrEmpty(pageQuery))
            {
                this.PageNumber = int.TryParse(pageQuery.ToString(), out int page) ? page : 1;
            }
            else
            {
                this.PageNumber = 1;
            }

            // Loads the author with their cheeps and followers using the authors name
            Author author = await this.authorRepository.FindAuthorWithName(authorName);

            // Creates a list to gather the author and all its followers
            var allAuthors = new List<Author> { author };

            // Adds all the followers to the list
            allAuthors.AddRange(author.FollowedAuthors ?? Enumerable.Empty<Author>());

            // Ensure PageNumber is valid and greater than 0
            this.PageNumber = Math.Max(1, this.PageNumber); // This ensures PageNumber is never less than 1

            // Sorts and converts the cheeps into cheepdto
            List<CheepDTO> cheeps = allAuthors
                .SelectMany(a => a.Cheeps ?? Enumerable.Empty<Cheep>())
                .OrderByDescending(cheep => cheep.TimeStamp)
                .Skip((this.PageNumber - 1) * this.PageSize)
                .Take(this.PageSize)
                .Select(cheep => new CheepDTO
                {
                    AuthorName = cheep.Author != null ? cheep.Author.Name : "Unknown",
                    Text = cheep.Text,
                    TimeStamp = cheep.TimeStamp.ToString(),
                    Likes = cheep.Likes,
                })
                .ToList();

            // Assign the combined list to Cheeps
            this.Cheeps = cheeps;
            if (this.User.Identity?.IsAuthenticated == true)
            {
                var authorEmail = this.User.FindFirst(ClaimTypes.Name)?.Value;

                // Check if authorEmail is null or empty
                if (string.IsNullOrEmpty(authorEmail))
                {
                    // Throw an exception if the email is missing
                    throw new InvalidOperationException("User's email is missing or not authenticated.");
                }

                // Proceed with the method call if the email is valid
                var loggedInAuthor = await this.authorRepository.FindAuthorWithEmail(authorEmail);
                this.FollowedAuthors = await this.authorRepository.GetFollowing(loggedInAuthor.Id);
            }

            return this.Page();
        }
        else
        {
            // Only loads the cheep that the author has written
            Author author = await this.authorRepository.FindAuthorWithName(pageUser);

            List<CheepDTO> cheeps = author.Cheeps?
                .OrderByDescending(cheep => cheep.TimeStamp)
                .Skip((this.PageNumber - 1) * this.PageSize)
                .Take(this.PageSize)
                .Select(cheep => new CheepDTO
                {
                    AuthorName = cheep.Author != null ? cheep.Author.Name : "Unknown",
                    Text = cheep.Text,
                    TimeStamp = cheep.TimeStamp.ToString(),
                    Likes = cheep.Likes,
                })
                .ToList() ?? new List<CheepDTO>(); // If Cheeps is null, use an empty list

            this.Cheeps = cheeps;
            if (this.User.Identity?.IsAuthenticated == true)
            {
                var authorEmail = this.User.FindFirst(ClaimTypes.Name)?.Value;

                // Check if authorEmail is null or empty
                if (string.IsNullOrEmpty(authorEmail))
                {
                    // Throw an exception if the email is missing
                    throw new InvalidOperationException("User's email is missing or not authenticated.");
                }

                // Proceed with the method call if the email is valid
                var loggedInAuthor = await this.authorRepository.FindAuthorWithEmail(authorEmail);
                this.FollowedAuthors = await this.authorRepository.GetFollowing(loggedInAuthor.Id);
            }

            return this.Page();
        }
    }

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

        Author author = await this.authorRepository.FindAuthorWithName(authorName);

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

        var author = await this.authorRepository.FindAuthorWithEmail(authorName);

        // Finds the author that the logged in author wants to follow
        var followAuthor = await this.authorRepository.FindAuthorWithName(followAuthorName);

        await this.authorRepository.FollowUserAsync(author.Id, followAuthor.Id);

        // updates the current author's list of followed authors
        this.FollowedAuthors = await this.authorRepository.GetFollowing(author.Id);

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

        var author = await this.authorRepository.FindAuthorWithName(authorName);

        // Finds the author that the logged in author wants to follow
        var followAuthor = await this.authorRepository.FindAuthorWithName(followAuthorName);

        await this.authorRepository.UnFollowUserAsync(author.Id, followAuthor.Id);

        // updates the current author's list of followed authors
        this.FollowedAuthors = await this.authorRepository.GetFollowing(author.Id);

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

        var author = await this.authorRepository.FindAuthorWithName(authorName);
        var cheep = await this.cheepRepository.FindCheep(text, timeStamp, cheepAuthorName);

        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }

        return await this.cheepRepository.DoesUserLikeCheep(cheep, author);
    }
}
