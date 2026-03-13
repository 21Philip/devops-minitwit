using Chirp.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Chirp.Infrastructure.Test.Unit;

public class TestDatabaseFactory : IAsyncDisposable
{
    public CheepDBContext DbContext { get; private set; } = null!;
    public AuthorRepository AuthorRepository { get; private set; } = null!;
    public CheepRepository CheepRepository { get; private set; } = null!;
    private Mock<UserManager<Author>> _userManagerMock;

    public static async Task<TestDatabaseFactory> CreateAsync(Action<Mock<UserManager<Author>>>? mockOptions = null)
    {
        var db = new TestDatabaseFactory(mockOptions);
        await db.InitializeAsync();
        return db;
    }

    private TestDatabaseFactory(Action<Mock<UserManager<Author>>>? mockOptions = null)
    {
        var store = new Mock<IUserStore<Author>>();
        _userManagerMock = new Mock<UserManager<Author>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<Author>>(),
            Array.Empty<IUserValidator<Author>>(),
            Array.Empty<IPasswordValidator<Author>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<Author>>>()
        );

        if (mockOptions is not null)
            mockOptions.Invoke(_userManagerMock);
    }

    public async Task InitializeAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseSqlite(connection)
            .Options;

        DbContext = new CheepDBContext(options);
        await DbContext.Database.EnsureCreatedAsync();

        AuthorRepository = new AuthorRepository(DbContext, _userManagerMock.Object);
        CheepRepository = new CheepRepository(DbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}