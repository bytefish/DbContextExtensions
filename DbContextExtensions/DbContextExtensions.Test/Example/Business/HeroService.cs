// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Context;
using DbContextExtensions.Scope;
using DbContextExtensions.Test.Example.Entities;
using DbContextExtensions.Test.Example.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Example.Business
{
    public class HeroService : IHeroService
    {
        private readonly IDbContextScopeFactory<ApplicationDbContext> dbContextScopeFactory;
        private readonly IHeroRepository heroRepository;

        public HeroService(IDbContextScopeFactory<ApplicationDbContext> dbContextScopeFactory, IHeroRepository heroRepository)
        {
            this.dbContextScopeFactory = dbContextScopeFactory;
            this.heroRepository = heroRepository;
        }

        public async Task AddHero(Hero hero, CancellationToken cancellationToken = default)
        {
            if(hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            using(var scope = dbContextScopeFactory.Create())
            {
                await heroRepository.AddHeroAsync(hero, cancellationToken);

                scope.Complete();
            }
        }

        public async Task<List<Hero>> GetHeroes(CancellationToken cancellationToken = default)
        {
            using (var scope = dbContextScopeFactory.Create(isReadOnly: true))
            {
                return await heroRepository.GetAllHeroesWithSuperpowersAsync(cancellationToken);
            }
        }
    }
}
