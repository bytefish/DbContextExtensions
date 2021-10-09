// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Context;
using DbContextExtensions.Mappings;
using DbContextExtensions.Scope;
using DbContextExtensions.Test.Example.Business;
using DbContextExtensions.Test.Example.Database;
using DbContextExtensions.Test.Example.Entities;
using DbContextExtensions.Test.Example.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Example.App
{
    public class SampleApplicationTests : AbstractTestBase
    {
        [SetUp]
        public void Setup()
        {
            var dbContextFactory = GetService<IDbContextFactory<ApplicationDbContext>>();

            using (var applicationDbContext = dbContextFactory.CreateDbContext())
            {
                applicationDbContext.Database.EnsureDeleted();
                applicationDbContext.Database.EnsureCreated();
            }
        }

        [Test]
        public async Task ExecuteService()
        {
            var heroService = GetService<IHeroService>();
            var loggerFactory = GetService<ILoggerFactory>();

            {
                var hero = new Hero
                {
                    Name = "Magneto",
                    Superpowers = new[]
                    {
                        new Superpower { Name = "Magnetism", Description = "Can control Magnetism."},
                        new Superpower { Name = "Sarcasm", Description = "Can turn irony to sarcasm."},
                    }
                };

                await heroService.AddHero(hero);
            }

            var heroes = await heroService.GetHeroes();

            foreach (var hero in heroes)
            {
                loggerFactory
                    .CreateLogger<SampleApplicationTests>()
                    .LogInformation($"Created Hero: {hero}");
            }

            Assert.AreEqual(1, heroes.Count);

            Assert.GreaterOrEqual(1, heroes[0].Id); 
            Assert.AreEqual("Magneto", heroes[0].Name);

            Assert.IsNotNull(heroes[0].Superpowers);
            Assert.AreEqual(2, heroes[0].Superpowers.Count);

            Assert.AreEqual(true, heroes[0].Superpowers.Any(x => string.Equals(x.Name, "Magnetism")));
            Assert.AreEqual(true, heroes[0].Superpowers.Any(x => string.Equals(x.Name, "Sarcasm")));
        }

        protected override void RegisterDependencies(ServiceCollection services)
        {
            // Logging:
            services.AddLogging();

            // Configure the DbContextFactory, which instantiates the DbContext:
            services.AddDbContextFactory<ApplicationDbContext>((services, options) =>
            {
                // Access the Unit Tests Configuration, which is configured by the Container:
                var configuration = services.GetRequiredService<IConfiguration>();

                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            // Register the Mappings:
            services.AddSingleton<IEntityMap, HeroEntityMap>();
            services.AddSingleton<IEntityMap, SuperpowerEntityMap>();
            services.AddSingleton<IEntityMap, HeroSuperpowerEntityMap>();

            // Register Scoping dependencies:
            services.AddSingleton<IDbContextAccessor, DbContextAccessor>();
            services.AddSingleton<IDbContextScopeFactory<ApplicationDbContext>, DbContextScopeFactory<ApplicationDbContext>>();

            // Register the Repositories:
            services.AddSingleton<IHeroRepository, HeroRepository>();

            // Register the Services:
            services.AddSingleton<IHeroService, HeroService>();
        }
    }
}