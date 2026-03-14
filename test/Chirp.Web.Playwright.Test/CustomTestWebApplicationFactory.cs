using System.Data.Common;
using Chirp.Infrastructure;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Chirp.Web.Playwright.Test;
/* Custom test environment for tests in ASP.NET Core with Playwright.
Defines a custom factory for the test server environment for the application
Referenced from: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0
 */

public class CustomTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private IHost? _host;
    private static readonly Queue<int> _portQueue = new Queue<int>(Enumerable.Range(5000, 20)); // Range af porte, f.eks. 5000-5999
    private readonly PostgreSqlContainer _postgres;

    private static int GetNextAvailablePort()
    {
        lock (_portQueue)
        {
            if (_portQueue.Count > 0) return _portQueue.Dequeue(); // Hent næste port

            // Hvis køen er tom, så kør en exception eller reinitialize køen
            throw new InvalidOperationException("No available ports left in the range.");
        }
    }

    // Property for getting the server's base address
    public string ServerAddress
    {
        get
        {
            if (_host is null) 
            {
                // This forces WebApplicationFactory to bootstrap the server
                using var client = CreateDefaultClient();
            }
            return ClientOptions.BaseAddress.ToString();
        }
    }

    public CustomTestWebApplicationFactory()
    {
        _postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("playwrightdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync() 
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure the database has spun up
        if (_postgres.State != TestcontainersStates.Running)
            _postgres.StartAsync().GetAwaiter().GetResult();

        builder.ConfigureServices(services =>
        {
            // Replace old dbcontext
            var ctxDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CheepDBContext>));
            if (ctxDescriptor != null) services.Remove(ctxDescriptor);

            var connDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (connDescriptor != null) services.Remove(connDescriptor);

            var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                IncludeErrorDetail = true
            }.ToString();

            services.AddDbContext<CheepDBContext>(options =>
                options.UseNpgsql(connectionString));

            // Replace old auth
            var authDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationService));
            if (authDescriptor != null) services.Remove(authDescriptor);

            services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme);

            // Replace old dataprotection
            var dpDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DataProtection") == true)
                .ToList();
            foreach (var d in dpDescriptors) services.Remove(d);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            // Create tables
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            db.Database.EnsureCreated();

            // Seed database
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            DBSeeder.Seed(dbContext);
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        var port = GetNextAvailablePort();
        var baseUrl = $"http://127.0.0.1:{port}";

        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel().UseUrls(baseUrl));

        _host = builder.Build();
        _host.Start();

        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("No server addresses found.");
        ClientOptions.BaseAddress = addresses.Addresses.Select(x => new Uri(x)).Last();

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _host?.StopAsync().Wait();
        Thread.Sleep(2000);
        _host?.Dispose();
    }
}
