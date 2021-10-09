// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Test.Example.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Example.Repositories
{
    public interface IHeroRepository
    {
        /// <summary>
        /// Adds a Hero to the database.
        /// </summary>
        /// <param name="hero">Hero and possibly properties</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>An awaitable Task</returns>
        Task AddHeroAsync(Hero hero, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a list of Heroes with their respective superpowers.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>An awaitable Task with the list of heroes and their superpowers</returns>
        Task<List<Hero>> GetAllHeroesWithSuperpowersAsync(CancellationToken cancellationToken);
    }
}