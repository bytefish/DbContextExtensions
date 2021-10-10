using DbContextExtensions.Test.Example.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Example.Business
{
    public interface IHeroService
    {
        /// <summary>
        /// Adds a new Hero asynchronously.
        /// </summary>
        /// <param name="hero">Hero</param>
        /// <param name="cancellationToken">CancellationToken to cancel from within async code</param>
        /// <returns>An awaitable Task</returns>
        Task AddHero(Hero hero, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all Heroes available.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to cancel from within async code</param>
        /// <returns>List of all Heroes</returns>
        Task<List<Hero>> GetHeroes(CancellationToken cancellationToken = default);
    }
}
