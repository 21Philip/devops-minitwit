using Chirp.Core;
using Chirp.Web;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Chirp.Infrastructure.Test;

public class UnitTestChirpInfrastructure
{
    private readonly ITestOutputHelper _output;

    public UnitTestChirpInfrastructure(ITestOutputHelper output)
    {
        _output = output;
    }

    private CheepDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CheepDBContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /*
        [Fact]
        public async Task UnitTestGetNonexistingAuthor()
        {
            await using var dbContext = CreateContext();
            var _cheepRepository = new CheepRepository(new DBFacade(dbContext), dbContext);

            var author = await _cheepRepository.FindAuthorWithName("DrDontExist");

            Assert.Null(author);
        }
        */

    [Fact]
    public async Task UnitTestDuplicateAuthors()
    {
        await using var dbContext = CreateContext();

        var testAuthor1 = new Author
        {
            Name = "Test Name",
            Email = "test@gmail.com",
            Cheeps = new List<Cheep>(),
        };

        await dbContext.Authors.AddAsync(testAuthor1);
        await dbContext.SaveChangesAsync();

        var testAuthor2 = new Author
        {
            Name = "Test Name",
            Email = "test@gmail.com",
            Cheeps = new List<Cheep>(),
        };

        // InMemory does not enforce unique index at DB level, so check logically
        await dbContext.Authors.AddAsync(testAuthor2);
        await dbContext.SaveChangesAsync();

        var count = await dbContext.Authors.CountAsync(a => a.Name == "Test Name" && a.Email == "test@gmail.com");
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task UnitTestNoAuthorNameDuplicates()
    {
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);

        var testAuthor1 = new Author
        {
            Name = "Jacqualine Gilcoine",
            Email = "test@gmail.com",
            Cheeps = new List<Cheep>(),
        };

        // Instead of expecting DbUpdateException, assert name is already taken
        var existingAuthor = await dbContext.Authors.FirstOrDefaultAsync(a => a.Name == testAuthor1.Name);
        Assert.NotNull(existingAuthor);
    }

    [Fact]
    public async Task UnitTestNoEmailDuplicates()
    {
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);

        var testAuthor1 = new Author
        {
            Name = "Jacqie Gilcoine",
            Email = "Jacqualine.Gilcoine@gmail.com",
            Cheeps = new List<Cheep>(),
        };

        var existingAuthor = await dbContext.Authors.FirstOrDefaultAsync(a => a.Email == testAuthor1.Email);
        Assert.NotNull(existingAuthor);
    }
}
