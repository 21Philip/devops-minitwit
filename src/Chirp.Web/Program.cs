using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Chirp.Web.Monitoring;


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

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("minitwit");

builder.Services.AddRazorPages();

// Register Prometheus metrics and hosted service
builder.Services.AddHostedService<CpuGaugeService>();

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

//This makes the program public, then the test class can access it
public partial class Program { }