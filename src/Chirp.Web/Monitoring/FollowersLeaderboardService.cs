// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Monitoring;

public class FollowersLeaderboardService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<FollowersLeaderboardService> logger;

    public FollowersLeaderboardService(IServiceScopeFactory scopeFactory, ILogger<FollowersLeaderboardService> logger)
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

                var followers = await db.Authors
                    .Where(a => a.Name != null)
                    .Select(a => new { Name = a.Name!, Count = a.Followers!.Count })
                    .ToListAsync(stoppingToken);

                foreach (var row in followers)
                {
                    Chirp.Web.Monitoring.Metrics.UserFollowers.WithLabels(row.Name).Set(row.Count);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to update follower leaderboard metric");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}