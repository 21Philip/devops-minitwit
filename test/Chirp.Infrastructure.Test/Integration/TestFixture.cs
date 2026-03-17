// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Chirp.Infrastructure.Test.Integration;

public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres;

    public IntegrationTestFixture()
    {
        this.postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("integrationdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await this.postgres.StartAsync();

        using var scope = this.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();

        await DBSeeder.Seed(dbContext);
    }

    /// <inheritdoc/>
    public new async Task DisposeAsync()
    {
        await this.postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CheepDBContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<CheepDBContext>(options =>
                options.UseNpgsql(this.postgres.GetConnectionString()));

            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<ICheepRepository, CheepRepository>();

            // Replace existing data protection with ephemeral one.
            var dpDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DataProtection") == true)
                .ToList();

            foreach (var d in dpDescriptors)
            {
                services.Remove(d);
            }

            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}