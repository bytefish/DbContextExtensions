// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Context;
using DbContextExtensions.Scope;
using DbContextExtensions.Test.Example.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Example.Repositories
{
    public class HeroRepository : IHeroRepository
    {
        private readonly ILogger<HeroRepository> logger;
        private readonly IDbContextAccessor dbContextAccessor;

        public HeroRepository(ILogger<HeroRepository> logger, IDbContextAccessor dbContextAccessor)
        {
            this.logger = logger;
            this.dbContextAccessor = dbContextAccessor;
        }

        public async Task AddHeroAsync(Hero hero, CancellationToken cancellationToken)
        {
            logger.LogDebug("Adding {@Hero} ...", hero);

            await Context.AddAsync(hero, cancellationToken);
        }

        public Task<List<Hero>> GetAllHeroesWithSuperpowersAsync(CancellationToken cancellationToken)
        {
            return Context.Set<Hero>()
                .Include(x => x.Superpowers)
                .ToListAsync(cancellationToken);
        }

        protected ApplicationDbContext Context => dbContextAccessor.GetDbContext<ApplicationDbContext>();
    }
}
