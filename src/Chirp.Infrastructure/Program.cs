// Copyright (c) devops-gruppe-connie. All rights reserved.
/*
* This program is only used for migrating the database.
*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chirp.Infrastructure;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                services.AddDbContext<CheepDBContext>(options =>
                    options.UseNpgsql(ctx.Configuration.GetConnectionString("DefaultConnection")));
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
        await db.Database.MigrateAsync();
    }
}