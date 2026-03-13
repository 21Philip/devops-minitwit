using Chirp.Core; // Ensure this includes Author and CheepDBContext
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Chirp.Infrastructure.Test;

public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly ITestOutputHelper _output;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly AuthorRepository _authorRepository;
    private readonly CheepRepository _cheepRepository;

    public IntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });

        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        _authorRepository = services.GetRequiredService<AuthorRepository>();
        _cheepRepository = services.GetRequiredService<CheepRepository>();
    }


    [Fact]
    public async Task CanAccessHomePage()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/");

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Failed to access home page. Status code: {response.StatusCode}, Response content: {content}");
        }

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task FindTimelineByAuthor()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/test_name0");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        _output.WriteLine("content: {0}", content);
        Assert.Contains("test_cheep0", content);
        Assert.DoesNotContain("test_cheep1", content);
    }

    [Fact]
    public async Task IfNoAuthorsAreFoundShowNoAuthors()
    {
        // Act
        string searchWord = "12345æøå";

        HttpResponseMessage response = await _client.GetAsync($"/SearchResults?searchWord={searchWord}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        _output.WriteLine("content: {0}", content);
        for (int i = 0; i < CustomWebApplicationFactory.SEED_AMOUNT; i++)
        {
            Assert.DoesNotContain($"test_name{i}", content);
        }
    }

    [Fact]
    public async Task IfOnFirstPageCantGoToPreviousPage()
    {
        HttpResponseMessage response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        _output.WriteLine("content: {0}", content);

        Assert.DoesNotContain("Previous", content);
    }

    [Fact]
    public async Task IfOnFirstPageCanGoToNextPage()
    {
        HttpResponseMessage response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        _output.WriteLine("content: {0}", content);

        Assert.Contains("Next", content);
    }

    [Fact]
    public async Task IfOnSecondPageCanGoToNextAndPreviousPage()
    {
        HttpResponseMessage response = await _client.GetAsync("/?page=2");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        _output.WriteLine("content: {0}", content);

        Assert.Contains("Next", content);
        Assert.Contains("Previous", content);
    }

    [Fact]
    public async Task IfOnLastPageCantGoToNextPage()
    {
        // Arrange (this is very fragile)
        int pageSize = 32;
        int page = CustomWebApplicationFactory.SEED_AMOUNT/pageSize;

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/?page={page}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        _output.WriteLine("content: {0}", content);
        Assert.DoesNotContain("Next", content);
    }

    [Fact]
    public async Task WhenLoggedOutCannotFollowUsers()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/");
        HttpResponseMessage response2 = await _client.GetAsync("/test_name0");

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        _output.WriteLine("content: {0}", content);

        Assert.DoesNotContain("Follow", content);
        Assert.DoesNotContain("Unfollow", content);
        Assert.DoesNotContain("Follow", content2);
        Assert.DoesNotContain("Unfollow", content2);
    }

    [Fact]
    public async Task CanCreateUserAndFindUser()
    {
        // Arrange
        string email = "Jacqualine.Gilcoine@gmail.com";
        await _authorRepository.CreateAuthor(email, "Jacqualine Gilcoine", "123");

        Author testAuthor = await _authorRepository.FindAuthorWithEmail(email);
        Cheep testCheep = new Cheep()
        {
            Text = "Lorem ipsum dolor sit amet",
            TimeStamp = DateTime.UtcNow,
            Author = testAuthor,
            AuthorId = testAuthor.Id
        };

        await _cheepRepository.SaveCheep(testCheep, testAuthor);

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/{testAuthor.Name}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        _output.WriteLine("content: {0}", content);
        Assert.Contains("Chirp!", content);
        Assert.Contains("Jacqualine", content);
    }

    [Fact]
    public async Task UserCanSearchForAuthors()
    {
        // Act
        string searchWord = "jacq";

        HttpResponseMessage response = await _client.GetAsync($"/SearchResults?searchWord={searchWord}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        _output.WriteLine("content: {0}", content);
        Assert.Contains("Jacqualine", content);
    }
}
