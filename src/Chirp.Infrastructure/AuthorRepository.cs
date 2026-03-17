// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure
{
    public interface IAuthorRepository
    {
        Task<Author> FindAuthorWithName(string userName);

        Task<Author?> FindAuthorWithNameNullable(string userName);

        Task<Author> FindAuthorWithEmail(string email);

        Task<bool> IsFollowingAsync(int followerId, int followedId);

        Task<List<Author>> GetFollowing(int followerId);

        Task<bool> FindIfAuthorExistsWithEmail(string email);

        Task FollowUserAsync(int followerId, int followedId);

        Task UnFollowUserAsync(int followerId, int followedId);

        Task<List<Cheep>> GetLikedCheeps(int userId);

        Task<List<AuthorDTO>> SearchAuthorsAsync(string searchWord);

        Task<bool> CreateAuthor(string email, string name, string password);

        Task<bool> DeleteAuthor(Author author);
    }

    /// <summary>
    /// Repository for author-related operations.
    /// </summary>
    public class AuthorRepository : IAuthorRepository
    {
        private readonly CheepDBContext dbContext;
        private readonly UserManager<Author> userManager;

        public AuthorRepository(CheepDBContext dbContext, UserManager<Author> userManager)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }

        /// <summary>
        /// Retrieves an author along with their relationships (e.g., followers, followed authors, liked Cheeps) using a username.
        /// </summary>
        /// <param name="userName">The username of the author to retrieve.</param>
        /// <returns>An <see cref="Author"/> object with associated relationships.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no author with the specified name exists.</exception>
        public async Task<Author> FindAuthorWithName(string userName)
        {
            var author = await this.dbContext.Authors
                .Include(a => a.FollowedAuthors!)
                .ThenInclude(fa => fa.Cheeps)
                .Include(a => a.Cheeps)
                .Include(a => a.Followers)
                .Include(a => a.LikedCheeps)
                .AsSplitQuery()
                .FirstOrDefaultAsync(author => author.Name == userName);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with name {userName} not found.");
            }

            return author;
        }

        /// <summary>
        /// Retrieves and author (without relationsships) by their username. May return null.
        /// </summary>
        /// <param name="userName">The username of the author to retrieve.</param>
        /// <returns>An <see cref="Author"/> object if exists; otherwise <c>null</c>.</returns>
        public async Task<Author?> FindAuthorWithNameNullable(string userName)
        {
            var author = await this.dbContext.Authors.FirstOrDefaultAsync(author => author.Name == userName);
            return author;
        }

        /// <summary>
        /// Finds an author by their email address.
        /// </summary>
        /// <param name="email">The email address of the author.</param>
        /// <returns>An <see cref="Author"/> object if found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no author with the given email exists.</exception>
        public async Task<Author> FindAuthorWithEmail(string email)
        {
            var author = await this.dbContext.Authors.FirstOrDefaultAsync(author => author.Email == email);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with email {email} not found.");
            }

            return author;
        }

        /// <summary>
        /// Checks whether an author exists with the specified email.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns><c>true</c> if an author exists with the specified email; otherwise, <c>false</c>.</returns>
        public async Task<bool> FindIfAuthorExistsWithEmail(string email)
        {
            var author = await this.dbContext.Authors.FirstOrDefaultAsync(author => author.Email == email);
            if (author == null)
            {
                return false;
            }

            return true;
        }

        public async Task<Author> FindAuthorWithId(int authorId)
        {
            var author = await this.dbContext.Authors.FirstOrDefaultAsync(author => author.Id == authorId);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with ID {authorId} was not found.");
            }

            return author;
        }

        /// <summary>
        /// Adds a follower-followed relationship between two authors.
        /// </summary>
        /// <param name="followerId">The ID of the user who is following.</param>
        /// <param name="followedId">The ID of the user being followed.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if either the follower or the followed user does not exist or has a null name.
        /// </exception>
        /// <remarks>
        /// This method verifies the relationship using <see cref="IsFollowingAsync"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task FollowUserAsync(int followerId, int followedId)
        {
            // logged in user
            var follower = await this.dbContext.Authors.SingleOrDefaultAsync(a => a.Id == followerId);

            // the user that the logged in user wants to follow
            var followed = await this.dbContext.Authors.SingleOrDefaultAsync(a => a.Id == followedId);

            if (follower == null || follower.Name == null)
            {
                throw new InvalidOperationException("Follower or follower's name is null.");
            }

            if (followed == null || followed.Name == null)
            {
                throw new InvalidOperationException("Followed author or followed author's name is null.");
            }

            if (!await this.IsFollowingAsync(followerId, followedId))
            {
                follower.FollowedAuthors?.Add(followed);
                followed.Followers?.Add(follower);
                await this.dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Removes a follower-followed relationship between two authors.
        /// </summary>
        /// <param name="followerId">The ID of the user who is unfollowing.</param>
        /// <param name="followedId">The ID of the user being unfollowed.</param>
        /// <remarks>
        /// This method loads the <c>FollowedAuthors</c> list to be able to remove the relationship.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UnFollowUserAsync(int followerId, int followedId)
        {
            // The logged in Author
            var follower = await this.dbContext.Authors
                .Include(a => a.FollowedAuthors)
                .AsSplitQuery()
                .SingleOrDefaultAsync(a => a.Id == followerId);

            // The author whom the logged in author is unfollowing
            var followed = await this.dbContext.Authors
                .SingleOrDefaultAsync(a => a.Id == followedId);

            if (follower != null && followed != null)
            {
                if (follower.FollowedAuthors?.Contains(followed) == true)
                {
                    follower.FollowedAuthors.Remove(followed);
                    await this.dbContext.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Checks if one user is following another.
        /// </summary>
        /// <param name="followerId">The ID of the user who might be following.</param>
        /// <param name="followedId">The ID of the user who might be followed.</param>
        /// <returns><c>true</c> if the follower-followed relationship exists; otherwise, <c>false</c>.</returns>
        public async Task<bool> IsFollowingAsync(int followerId, int followedId)
        {
            var loggedInUser = await this.dbContext.Authors.Include(a => a.FollowedAuthors)
                .Include(a => a.FollowedAuthors)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == followerId);

            return loggedInUser?.FollowedAuthors?.Any(f => f.Id == followedId) ?? false;
        }

        /// <summary>
        /// Retrieves the list of authors a specific user is following.
        /// </summary>
        /// <param name="followerId">The ID of the user whose following list to retrieve.</param>
        /// <returns>A list of <see cref="Author"/> objects that the user is following.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the user or their following list is null.</exception>
        public async Task<List<Author>> GetFollowing(int followerId)
        {
            var follower = await this.dbContext.Authors.Include(a => a.FollowedAuthors)
                .Include(a => a.FollowedAuthors)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == followerId);
            if (follower == null || follower.FollowedAuthors == null)
            {
                throw new InvalidOperationException("Follower or followed authors is null.");
            }

            return follower.FollowedAuthors;
        }

        /// <summary>
        /// Retrieves all Cheeps liked by a user.
        /// </summary>
        /// <param name="userId">The ID of the user whose liked Cheeps the method retrieves.</param>
        /// <returns>A list of <see cref="Cheep"/> objects liked by the user.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the user or their liked Cheeps list is null.</exception>
        public async Task<List<Cheep>> GetLikedCheeps(int userId)
        {
            var user = await this.dbContext.Authors
                .Include(a => a.LikedCheeps)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == userId);

            // user.LikedCheeps cannot be null here because the query ensures it's at least an empty list
            // we added the check so the compiler doesn't give us a warning in the return statement
            if (user == null || user.LikedCheeps == null)
            {
                throw new InvalidOperationException("User liked cheeps is null.");
            }

            return user.LikedCheeps;
        }

        /// <summary>
        /// Searches for authors based on a name fragment.
        /// </summary>
        /// <param name="searchWord">The partial or full name of the author to search for.</param>
        /// <returns>A list of <see cref="AuthorDTO"/> objects matching the search criteria.</returns>
        public async Task<List<AuthorDTO>> SearchAuthorsAsync(string searchWord)
        {
            if (string.IsNullOrWhiteSpace(searchWord))
            {
                return new List<AuthorDTO>(); // Return empty list if no search word is provided
            }

            if (searchWord.Length > 2)
            {
                // Perform a case-insensitive search for authors whose name contains the search word
                return await this.dbContext.Authors
                    .Where(a => EF.Functions.Like(a.Name, $"%{searchWord}%"))
                    .Select(a => new AuthorDTO
                    {
                        Name = a.Name, // Map Author entity to AuthorDTO
                    })
                    .ToListAsync();
            }
            else
            {
                return await this.dbContext.Authors
                    .Where(a => EF.Functions.Like(a.Name, $"{searchWord}%"))
                    .Select(a => new AuthorDTO
                    {
                        Name = a.Name, // Map Author entity to AuthorDTO
                    })
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Creates a new author/user in the database.
        /// </summary>
        /// <param name="email">The email of the new author/user.</param>
        /// <param name="name">The name of the new author/user.</param>
        /// <param name="password">The password of the new author/user.</param>
        /// <returns>True if the operation succeeded.</returns>
        public async Task<bool> CreateAuthor(string email, string name, string password)
        {
            var user = new Author
            {
                Email = email,
                UserName = email,
                Name = name,
#pragma warning disable SA1010, SA1003
                Cheeps = [], // SA1010, SA1003 conflict on collection expression syntax
#pragma warning restore SA1010, SA1003
            };

            IdentityResult result = await this.userManager.CreateAsync(user, password);
            return result.Succeeded;
        }

        /// <summary>
        /// Deletes an author/user from the database.
        /// </summary>
        /// <param name="author">The author to delete.</param>
        /// <returns>True if the operation succeeded.</returns>
        public async Task<bool> DeleteAuthor(Author author)
        {
            IdentityResult result = await this.userManager.DeleteAsync(author);
            return result.Succeeded;
        }
    }
}