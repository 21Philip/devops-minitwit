// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Web.Monitoring;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace Chirp.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Console logging and minimum level
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<CheepDBContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<ICheepRepository, CheepRepository>();
        builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();

        builder.Services.AddDefaultIdentity<Author>().AddEntityFrameworkStores<CheepDBContext>();

        builder.Services.AddDataProtection().PersistKeysToDbContext<CheepDBContext>();

        builder.Services.AddRazorPages();

        // Register Prometheus metrics and hosted service
        builder.Services.AddHostedService<CpuGaugeService>();
        builder.Services.AddHostedService<FollowersLeaderboardService>();

        var app = builder.Build();

        // Simple request logging middleware so each HTTP request/response is logged to stdout
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        app.Use(async (context, next) =>
        {
            logger.LogInformation("HTTP {Method} {Path} - starting", context.Request.Method, context.Request.Path);
            await next();
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
        });
        
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.UseHttpsRedirection(); // only enable HTTPS redirection outside Development
        }

        // Expose Prometheus metrics at /metrics
        app.UseRouting();
        app.UseHttpMetrics(); // built-in middleware from prometheus-net to collect request metrics

        app.UseStaticFiles();
        
        app.Use(async (context, next) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await next();
            }
            finally
            {
                sw.Stop();
                
                var endpoint = context.GetEndpoint();

                var route =
                    (endpoint as RouteEndpoint)?.RoutePattern?.RawText
                    ?? endpoint?.Metadata.GetMetadata<RouteEndpoint>()?.RoutePattern?.RawText
                    ?? endpoint?.DisplayName
                    ?? "unknown";

                var method = context.Request.Method;
                var status = context.Response.StatusCode.ToString();
                
                Chirp.Web.Monitoring.Metrics.HttpResponses.WithLabels(method, route, status).Inc();
                Chirp.Web.Monitoring.Metrics.HttpRequestDuration.WithLabels(method, route, status).Observe(sw.Elapsed.TotalSeconds);
            }
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapMetrics(); 
        app.MapRazorPages();

        app.Run();
    }
}
