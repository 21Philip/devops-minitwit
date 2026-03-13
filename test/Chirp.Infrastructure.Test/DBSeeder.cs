using Chirp.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Chirp.Infrastructure.Test;

public class DBSeeder
{
    public async static void Seed(IServiceProvider services)
    {
        var authorRepository = services.GetRequiredService<AuthorRepository>();
        var cheepRepository = services.GetRequiredService<CheepRepository>();

        for (int i = 0; i < CustomWebApplicationFactory.SEED_AMOUNT; i++)
        {
            string email = $"test_email{i}";
            await authorRepository.CreateAuthor(email, $"test_name{i}", "test_pwd");

            Author testAuthor = await authorRepository.FindAuthorWithEmail(email);
            Cheep testCheep = new Cheep()
            {
                Text = $"test_cheep{i}",
                TimeStamp = DateTime.UtcNow,
                Author = testAuthor,
                AuthorId = testAuthor.Id
            };  

            await cheepRepository.SaveCheep(testCheep, testAuthor);
        }
    }
}