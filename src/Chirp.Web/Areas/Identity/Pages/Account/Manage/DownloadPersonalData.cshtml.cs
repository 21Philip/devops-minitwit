// Copyright (c) devops-gruppe-connie. All rights reserved.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Areas.Identity.Pages.Account.Manage
{
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<Author> userManager;
        private readonly IAuthorRepository authorRepository;

        public DownloadPersonalDataModel(
            UserManager<Author> userManager,
            IAuthorRepository authorRepository)
        {
            this.userManager = userManager;
            this.authorRepository = authorRepository;
        }

        public IActionResult OnGet()
        {
            return this.NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
            }

            var authorName = this.User.FindFirst("Name")?.Value ?? "User";
            var author = await this.authorRepository.FindAuthorWithName(authorName);
            var baseUrl = $"{this.HttpContext.Request.Scheme}://{this.HttpContext.Request.Host}";
            var followedAuthorsLinks = author.FollowedAuthors?.Select(author =>
                $"{baseUrl}/{Uri.EscapeDataString(author.Name)}").ToList() ?? new List<string>();

            var data = new
            {
                Name = author.Name,
                Email = author.Email,
                Phonenumber = author.PhoneNumber,
                FollowedAuthors = followedAuthorsLinks,
                Cheeps = author.Cheeps,
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true, // Enables pretty printing
            };

            var jsonData = JsonSerializer.Serialize(data, options);
            var bytes = Encoding.UTF8.GetBytes(jsonData);
            return this.File(bytes, "application/json", "PersonalData.json");
        }
    }
}
