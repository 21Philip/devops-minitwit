// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.Reflection.Metadata;
using Chirp.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Chirp.Infrastructure.Test;

public class DBSeeder
{
    public const int SEEDAMOUNT = 100;

    public static async Task Seed(CheepDBContext dbContext)
    {
        for (int i = 0; i < SEEDAMOUNT; i++)
        {
            string email = $"test_email{i}";

            Author testAuthor = new ()
            {
                Email = email,
                UserName = email,
                Name = $"test_name{i}",
            };

            await dbContext.AddAsync(testAuthor);

            Cheep testCheep = new ()
            {
                Text = $"test_cheep{i}",
                TimeStamp = DateTime.UtcNow,
                Author = testAuthor,
                AuthorId = testAuthor.Id,
            };
            await dbContext.AddAsync(testCheep);

            await dbContext.SaveChangesAsync();
        }
    }
}