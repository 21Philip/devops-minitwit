// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages
{
    /// <summary>
    /// This class handles the functionality of the searchbar for finding authors.
    /// </summary>
    public class SearchResultsModel : PageModel
    {
        private readonly IAuthorRepository authorRepository;

        public SearchResultsModel(IAuthorRepository authorRepository)
        {
            this.authorRepository = authorRepository;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchWord { get; set; }

        public List<AuthorDTO> AuthorDTOs { get; set; } = new List<AuthorDTO>();

        /// <summary>
        /// Handles the GET request to search authors based on the search input.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnGet()
        {
            if (!string.IsNullOrEmpty(this.SearchWord))
            {
                this.AuthorDTOs = await this.authorRepository.SearchAuthorsAsync(this.SearchWord);
            }
        }
    }
}