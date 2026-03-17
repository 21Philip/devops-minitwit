// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Web.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Chirp.Web.Pages;

/// <summary>
/// This class handles the users interactions with the user's timeline page.
/// This includes posting and liking cheeps, as well as following/unfollowing author.
/// </summary>
public class UserTimelineModel : TimelineModel
{
    public UserTimelineModel(ICheepRepository cheepRepository, IAuthorRepository authorRepository)
        : base(cheepRepository, authorRepository)
    {
    }

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
            Author author = await this.AuthorRepository.FindAuthorWithName(authorName);

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
                var loggedInAuthor = await this.AuthorRepository.FindAuthorWithEmail(authorEmail);
                this.FollowedAuthors = await this.AuthorRepository.GetFollowing(loggedInAuthor.Id);
            }

            return this.Page();
        }
        else
        {
            // Only loads the cheep that the author has written
            Author author = await this.AuthorRepository.FindAuthorWithName(pageUser);

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
                var loggedInAuthor = await this.AuthorRepository.FindAuthorWithEmail(authorEmail);
                this.FollowedAuthors = await this.AuthorRepository.GetFollowing(loggedInAuthor.Id);
            }

            return this.Page();
        }
    }
}
