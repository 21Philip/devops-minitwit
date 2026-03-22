// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Core; // Ensure this includes Author and CheepDBContext
using Chirp.Infrastructure.Test;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Chirp.Infrastructure.Test.Integration;

public class IntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture fixture;
    private readonly ITestOutputHelper output;
    private readonly HttpClient client;

    public IntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        this.output = output;
        this.fixture = fixture;

        this.client = fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
        });
    }

    [Fact]
    public async Task CanAccessHomePage()
    {
        // Act
        HttpResponseMessage response = await this.client.GetAsync("/");

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine($"Failed to access home page. Status code: {response.StatusCode}, Response content: {content}");
        }

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task FindTimelineByAuthor()
    {
        // Act
        HttpResponseMessage response = await this.client.GetAsync("/test_name0");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);
        Assert.Contains("test_cheep0", content);
        Assert.DoesNotContain("test_cheep1", content);
    }

    [Fact]
    public async Task IfNoAuthorsAreFoundShowNoAuthors()
    {
        // Act
        string searchWord = "12345æøå";

        HttpResponseMessage response = await this.client.GetAsync($"/SearchResults?searchWord={searchWord}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);
        for (int i = 0; i < DBSeeder.SEEDAMOUNT; i++)
        {
            Assert.DoesNotContain($"test_name{i}", content);
        }
    }

    [Fact]
    public async Task IfOnFirstPageCantGoToPreviousPage()
    {
        HttpResponseMessage response = await this.client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        this.output.WriteLine("content: {0}", content);

        Assert.DoesNotContain("Previous", content);
    }

    [Fact]
    public async Task IfOnFirstPageCanGoToNextPage()
    {
        HttpResponseMessage response = await this.client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        this.output.WriteLine("content: {0}", content);

        Assert.Contains("Next", content);
    }

    [Fact]
    public async Task IfOnSecondPageCanGoToNextAndPreviousPage()
    {
        HttpResponseMessage response = await this.client.GetAsync("/?page=2");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        this.output.WriteLine("content: {0}", content);

        Assert.Contains("Next", content);
        Assert.Contains("Previous", content);
    }

    [Fact]
    public async Task IfOnLastPageCantGoToNextPage()
    {
        // Arrange (this is very fragile)
        int pageSize = 32;
        int page = (DBSeeder.SEEDAMOUNT / pageSize) + 1;

        // Act
        HttpResponseMessage response = await this.client.GetAsync($"/?page={page}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);
        Assert.DoesNotContain("Next", content);
    }

    [Fact]
    public async Task IfOnLastPageCanGoToPreviousPage()
    {
        // Arrange (this is very fragile)
        int pageSize = 32;
        int page = (DBSeeder.SEEDAMOUNT / pageSize) + 1;

        // Act
        HttpResponseMessage response = await this.client.GetAsync($"/?page={page}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);
        Assert.Contains("Previous", content);
    }

    [Fact]
    public async Task WhenLoggedOutCannotFollowUsers()
    {
        // Act
        HttpResponseMessage response = await this.client.GetAsync("/");
        HttpResponseMessage response2 = await this.client.GetAsync("/test_name0");

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);

        Assert.DoesNotContain("Follow", content);
        Assert.DoesNotContain("Unfollow", content);
        Assert.DoesNotContain("Follow", content2);
        Assert.DoesNotContain("Unfollow", content2);
    }

    [Fact]
    public async Task CanCreateUserAndFindUser()
    {
        using var scope = this.fixture.Services.CreateScope();
        var services = scope.ServiceProvider;

        var authorRepository = services.GetRequiredService<IAuthorRepository>();
        var cheepRepository = services.GetRequiredService<ICheepRepository>();

        // Arrange
        string email = "Jacqualine.Gilcoine@gmail.com";
        bool result = await authorRepository.CreateAuthor(email, "Jacqualine Gilcoine", "Test123_");

        Author testAuthor = await authorRepository.FindAuthorWithEmail(email);
        Cheep testCheep = new Cheep()
        {
            Text = "Lorem ipsum dolor sit amet",
            TimeStamp = DateTime.UtcNow,
            Author = testAuthor,
            AuthorId = testAuthor.Id,
        };

        await cheepRepository.SaveCheep(testCheep, testAuthor);

        // Act
        HttpResponseMessage response = await this.client.GetAsync($"/{testAuthor.Name}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);
        Assert.Contains("Jacqualine", content);
    }

    [Fact]
    public async Task UserCanSearchForAuthors()
    {
        // Act
        string searchWord = "Jacq";

        HttpResponseMessage response = await this.client.GetAsync($"/SearchResults?searchWord={searchWord}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        this.output.WriteLine("content: {0}", content);
        Assert.Contains("Jacqualine", content);
    }
}
