// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Web;
using Chirp.Web.Monitoring;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CheepDBContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICheepRepository, CheepRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();

builder.Services.AddDefaultIdentity<Author>().AddEntityFrameworkStores<CheepDBContext>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("minitwit");

builder.Services.AddRazorPages();

// Register Prometheus metrics and hosted service
builder.Services.AddHostedService<CpuGaugeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Expose Prometheus metrics at /metrics
app.UseRouting();
app.UseHttpMetrics(); // built-in middleware from prometheus-net to collect request metrics

app.UseHttpsRedirection();
app.UseStaticFiles();

// Custom middleware to measure request duration and increment response counter
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

        // Record duration in milliseconds
        Chirp.Web.Monitoring.Metrics.RequestDuration.Observe(sw.Elapsed.TotalMilliseconds);
        Chirp.Web.Monitoring.Metrics.ResponseCounter.Inc();
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapMetrics(); // maps /metrics
app.MapRazorPages();

app.Run();

// This makes the program public, then the test class can access it
public partial class Program
{
}
