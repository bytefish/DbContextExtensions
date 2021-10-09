using DbContextExtensions.Test.Example.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Example.Business
{
    public interface IHeroService
    {
        Task AddHero(Hero hero, CancellationToken cancellationToken = default);

        Task<List<Hero>> GetHeroes(CancellationToken cancellationToken = default);

    }
}
