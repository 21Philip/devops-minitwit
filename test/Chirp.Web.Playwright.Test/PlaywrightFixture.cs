// Copyright (c) devops-gruppe-connie. All rights reserved.

/* Custom test environment for tests in ASP.NET Core with Playwright.
Defines a custom factory for the test server environment for the application
Referenced from: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0 */

using System.Data.Common;
using System.Net;
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
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace Chirp.Web.Playwright.Test;

public class PlaywrightFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const float TIMEOUTMS = 30000;
    private readonly PostgreSqlContainer postgres;
    private Respawner? respawner;
    private string? connectionString;
    private IHost? host;

    public PlaywrightFixture()
    {
        this.postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("playwrightdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public string BaseURL { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await this.postgres.StartAsync();
        this.connectionString = new NpgsqlConnectionStringBuilder(this.postgres.GetConnectionString())
        {
            IncludeErrorDetail = true,
        }.ToString();

        // Force URL/address assignment
        this.CreateClient();

        // Setup Respawn
        var connection = new NpgsqlConnection(this.connectionString);
        await connection.OpenAsync();

        this.respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
        });
    }

    /// <inheritdoc/>
    public new async Task DisposeAsync()
    {
        await this.postgres.DisposeAsync();
        this.host!.Dispose();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to the inital seeding.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public async Task ResetDatabaseAsync()
    {
        // Empty
        var connection = new NpgsqlConnection(this.connectionString);
        await connection.OpenAsync();
        await this.respawner!.ResetAsync(connection);

        // Re-seed
        using var scope = this.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
        DBSeeder.Seed(dbContext);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure the database has spun up
        if (this.postgres.State != TestcontainersStates.Running)
        {
            this.postgres.StartAsync().GetAwaiter().GetResult();
        }

        builder.ConfigureServices(services =>
        {
            // Replace old dbcontext
            var ctxDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CheepDBContext>));
            if (ctxDescriptor != null)
            {
                services.Remove(ctxDescriptor);
            }

            var connDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (connDescriptor != null)
            {
                services.Remove(connDescriptor);
            }

            services.AddDbContext<CheepDBContext>(options =>
                options.UseNpgsql(this.connectionString));

            // Replace old auth
            var authDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationService));
            if (authDescriptor != null)
            {
                services.Remove(authDescriptor);
            }

            services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme);

            // Replace old dataprotection
            var dpDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DataProtection") == true)
                .ToList();
            foreach (var d in dpDescriptors)
            {
                services.Remove(d);
            }

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
        // Create a separate "real" host that listens on a real TCP port
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel(options => options.Listen(IPAddress.Loopback, 0))); // Port 0 = random available port

        this.host = builder.Build();
        this.host.Start();

        // Grab the actual port Kestrel bound to
        var server = this.host.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        this.BaseURL = $"{addressFeature!.Addresses.First()}/";

        return testHost;
    }
}
