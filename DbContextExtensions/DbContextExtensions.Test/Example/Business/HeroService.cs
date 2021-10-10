// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Context;
using DbContextExtensions.Scope;
using DbContextExtensions.Test.Example.Entities;
using DbContextExtensions.Test.Example.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace DbContextExtensions.Test.Example.Business
{
    /// <summary>
    /// Entity Framework-based <see cref="IHeroService"/> implementation.
    /// </summary>
    public class HeroService : IHeroService
    {
        private readonly IDbContextScopeFactory<ApplicationDbContext> dbContextScopeFactory;
        private readonly IHeroRepository heroRepository;

        public HeroService(IDbContextScopeFactory<ApplicationDbContext> dbContextScopeFactory, IHeroRepository heroRepository)
        {
            this.dbContextScopeFactory = dbContextScopeFactory;
            this.heroRepository = heroRepository;
        }

        /// <summary>
        /// Adds a new Hero asynchronously.
        /// </summary>
        /// <param name="hero">Hero</param>
        /// <param name="cancellationToken">CancellationToken to cancel from within async code</param>
        /// <returns>An awaitable Task</returns>
        public async Task AddHero(Hero hero, CancellationToken cancellationToken = default)
        {
            if(hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            using (var scope = dbContextScopeFactory.Create())
            {
                await heroRepository.AddHeroAsync(hero, cancellationToken);

                scope.Complete();
            }
        }

        /// <summary>
        /// Gets all Heroes available.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to cancel from within async code</param>
        /// <returns>List of all Heroes</returns>
        public async Task<List<Hero>> GetHeroes(CancellationToken cancellationToken = default)
        {
            using (var scope = dbContextScopeFactory.Create(isReadOnly: true))
            {
                return await heroRepository.GetAllHeroesWithSuperpowersAsync(cancellationToken);
            }
        }
    }
}
