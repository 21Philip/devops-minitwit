using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Chirp.Infrastructure.Test;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

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
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CheepDBContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<CheepDBContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            db.Database.EnsureCreated();

            // Replace existing data protection with ephemeral one.
            var dpDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DataProtection") == true)
                .ToList();

            foreach (var d in dpDescriptors)
                services.Remove(d);

            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });

        builder.UseEnvironment("Development");
    }
}