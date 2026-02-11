
using Chirp.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure
{
    public interface IGlobalIntRepository
    {
        Task<int?> Get(string key);
        Task Put(string key, int value);
    }

    public class GlobalIntRepository : IGlobalIntRepository
    {
        public readonly CheepDBContext _dbContext;

        public GlobalIntRepository(CheepDBContext dbContext)
        {
            _dbContext = dbContext;
            SQLitePCL.Batteries.Init();
        }

        /// <summary>
        /// Returns the integer assoiciated with the given key.
        /// </summary>
        /// <param name="key">The key to search by.</param>
        /// <returns>An integer if key was found, otherwise null</returns>
        public async Task<int?> Get(string key)
        {
            return await _dbContext.GlobalIntegers
                .Where(g => g.Key == key)
                .Select(g => (int?)g.Value)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Inserts the given key-value pair into the database. If the pair already exists
        /// the old value is replace by the new.
        /// </summary>
        /// <param name="key">The key of the pair./param>
        /// <param name="value">The value of the pair.</param>
        /// <returns>An integer if key was found, otherwise null</returns>
        public async Task Put(string key, int value)
        {
            var entity = new GlobalInteger { Key = key, Value = value };
            _dbContext.GlobalIntegers.Update(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}