// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Monitoring;

public class CheepsAndFollowersMetricsService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<CheepsAndFollowersMetricsService> logger;

    public CheepsAndFollowersMetricsService(IServiceScopeFactory scopeFactory, ILogger<CheepsAndFollowersMetricsService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = this.scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CheepDBContext>();

                // Count total cheeps in database
                var totalCheeps = await db.Cheeps.CountAsync(stoppingToken);
                Chirp.Web.Monitoring.Metrics.CheepsPosted.Set(totalCheeps);

                // Sum all followers across all authors
                var totalFollowers = await db.Authors
                    .SelectMany(a => a.Followers!)
                    .CountAsync(stoppingToken);
                Chirp.Web.Monitoring.Metrics.AuthorsFollowed.Set(totalFollowers);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to update cheeps and followers metrics");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}