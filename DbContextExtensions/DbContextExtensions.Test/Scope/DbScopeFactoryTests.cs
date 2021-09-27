using DbContextExtensions.Context;
using DbContextExtensions.Exceptions;
using DbContextExtensions.Mappings;
using DbContextExtensions.Scope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Test.Scope
{
    public class DbScopeFactoryTests : AbstractTestBase
    {
        // A Person to manage.
        class Person
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public DateTime BirthDate { get; set; }
        }

        // The Fluent EF Core Mapping.
        class PersonEntityMap : EntityMap<Person>
        {
            protected override void InternalMap(ModelBuilder model, EntityTypeBuilder<Person> entity)
            {
                model
                    .HasSequence("SeqPerson", seq_builder => seq_builder.IncrementsBy(10));

                entity
                    .ToTable("Person", "dbo")
                    .HasKey(x => x.Id);

                entity
                    .Property(x => x.Id)
                    .UseHiLo("SeqDocument")
                    .HasColumnName("PersonID");

                entity
                    .Property(x => x.FirstName)
                    .HasColumnName("FirstName");

                entity
                    .Property(x => x.LastName)
                    .HasColumnName("LastName");

                entity
                    .Property(x => x.BirthDate)
                    .HasColumnName("BirthDate");
            }
        }

        [SetUp]
        public void Setup()
        {
            var dbContextFactory = GetService<IDbContextFactory<ApplicationDbContext>>();

            using (var applicationDbContext = dbContextFactory.CreateDbContext())
            {
                applicationDbContext.Database.EnsureCreated();
            }
        }

        [TearDown]
        public void Teardown()
        {
            var dbContextFactory = GetService<IDbContextFactory<ApplicationDbContext>>();

            using (var applicationDbContext = dbContextFactory.CreateDbContext())
            {
                applicationDbContext.Database.EnsureDeleted();
            }
        }

        [Test]
        public async Task GuardAgainstDirectSavesWhenAllowSavingNotAllowedTest()
        {
            var dbContextScopeFactory = GetService<IDbContextScopeFactory<ApplicationDbContext>>();

            Exception thrown = null;

            try
            {
                using (var dbContextScope = dbContextScopeFactory.Create())
                {
                    var dbContext = dbContextScope.GetDbContext();

                    // Add some fake data:
                    await dbContext.Set<Person>()
                        .AddAsync(new Person());

                    // Try to directly save the data. This should throw:
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                thrown = e;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual(typeof(InvalidOperationException), thrown.GetType());
            Assert.AreEqual("Don't call SaveChanges directly on a context owned by a DbContextScope. Use DbContextScope.Commit instead or enable AllowSaving on creation.", thrown.Message);
        }

        [Test]
        public async Task DoNotGuardAgainstDirectSavesWhenAllowSavingIsAllowedTest()
        {

            var dbContextScopeFactory = GetService<IDbContextScopeFactory<ApplicationDbContext>>();

            Exception thrown = null;

            try
            {
                using (var dbContextScope = dbContextScopeFactory.Create(allowSaving: true))
                {
                    var dbContext = dbContextScope.GetDbContext();

                    // Add some fake data:
                    await dbContext.Set<Person>()
                        .AddAsync(new Person());

                    // Try to directly save the data. This should throw:
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                thrown = e;
            }

            Assert.IsNull(thrown);
        }

        [Test]
        public async Task OutermostDbContextScopeCommitsTransactionTest()
        {
            var dbContextScopeFactory = GetService<IDbContextScopeFactory<ApplicationDbContext>>();

            using (var dbContextScope0 = dbContextScopeFactory.Create())
            {
                using (var dbContextScope1 = dbContextScopeFactory.Create())
                {
                    var dbContext = dbContextScope1.GetDbContext();

                    // Add some fake data:
                    await dbContext.Set<Person>()
                        .AddAsync(new Person() { FirstName = "Philipp", LastName = "Wagner", BirthDate = new DateTime(2013, 1, 1) });

                    dbContextScope1.Complete();
                }

                dbContextScope0.Complete();
            }

            // Get and Assert the Results:
            using (var dbContextScope0 = dbContextScopeFactory.Create(isReadOnly: true))
            {
                // Get the underlying DbContext:
                var dbContext = dbContextScope0.GetDbContext();

                // Get Results:
                var results = await dbContext.Set<Person>().ToListAsync();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual("Philipp", results[0].FirstName);
                Assert.AreEqual("Wagner", results[0].LastName);
                Assert.AreEqual(new DateTime(2013, 1, 1), results[0].BirthDate);
            }
        }

        [Test]
        public async Task RollbackChangesFromNestedDbContextScopeTest()
        {
            var dbContextScopeFactory = GetService<IDbContextScopeFactory<ApplicationDbContext>>();

            Exception thrown = null;

            try
            {
                using (var dbContextScope0 = dbContextScopeFactory.Create())
                {
                    using (var dbContextScope1 = dbContextScopeFactory.Create())
                    {
                        var dbContext = dbContextScope1.GetDbContext();

                        // Add some fake data:
                        await dbContext.Set<Person>()
                            .AddAsync(new Person() { FirstName = "Philipp", LastName = "Wagner", BirthDate = new DateTime(2013, 1, 1) });
                    }

                    dbContextScope0.Complete();
                }
            } 
            catch(Exception e)
            {
                thrown = e;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual(typeof(DbContextScopeAbortedException), thrown.GetType());

            // Get and Assert the Results:
            using (var dbContextScope0 = dbContextScopeFactory.Create(isReadOnly: true))
            {
                // Get the underlying DbContext:
                var dbContext = dbContextScope0.GetDbContext();

                // Get Results:
                var results = await dbContext.Set<Person>().ToListAsync();

                Assert.AreEqual(0, results.Count);
            }
        }

        [Test]
        public async Task ExceptionInNestedScopeAbortsEntireScopeTest()
        {
            var dbContextScopeFactory = GetService<IDbContextScopeFactory<ApplicationDbContext>>();

            Exception thrown = null;

            try
            {
                using (var dbContextScope0 = dbContextScopeFactory.Create())
                {
                    var dbContext = dbContextScope0.GetDbContext();

                    // Add some fake data:
                    await dbContext
                        .Set<Person>()
                        .AddAsync(new Person() { FirstName = "Philipp", LastName = "Wagner", BirthDate = new DateTime(2013, 1, 1) });

                    using (var dbContextScope1 = dbContextScopeFactory.Create())
                    {
                        throw new Exception();
                    }
                }
            } 
            catch(Exception e)
            {
                thrown = e;
            }

            Assert.IsNotNull(thrown);

            // Get and Assert the Results:
            using (var dbContextScope0 = dbContextScopeFactory.Create(isReadOnly: true))
            {
                // Get the underlying DbContext:
                var dbContext = dbContextScope0.GetDbContext();

                // Get Results:
                var results = await dbContext.Set<Person>().ToListAsync();

                Assert.AreEqual(0, results.Count);
            }
        }

        /// <summary>
        /// Makes sure we cannot nest a Read/Write Scope within a ReadOnly-Scope.
        /// </summary>
        [Test]
        public void PreventNestingReadWriteScopeInReadOnlyScopeTest()
        {
            var dbContextScopeFactory = GetService<IDbContextScopeFactory<ApplicationDbContext>>();

            Exception thrown = null;

            try
            {
                using (var dbContextScopeReadOnly = dbContextScopeFactory.Create(isReadOnly: true))
                {
                    using (var dbContextScopeReadWrite = dbContextScopeFactory.Create(isReadOnly: false))
                    {
                        // Nothing to do, as we should already throw here...
                    }
                }
            }
            catch (Exception e)
            {
                thrown = e;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual(typeof(InvalidOperationException), thrown.GetType());
            Assert.AreEqual("Cannot nest a read/write DbContextScope within a read-only DbContextScope.", thrown.Message);
        }

        protected override void RegisterDependencies(ServiceCollection services)
        {
            // Logging:
            services.AddLogging();

            // Register the Mappings:
            services.AddSingleton<IEntityMap, PersonEntityMap>();

            // Register Scoping dependencies:
            services.AddSingleton<IDbContextAccessor, DbContextAccessor>();
            services.AddSingleton<IDbContextScopeFactory<ApplicationDbContext>, DbContextScopeFactory<ApplicationDbContext>>();

            // Configure the DbContextFactory, which instantiates the DbContext:
            services.AddDbContextFactory<ApplicationDbContext>((services, options) =>
            {
                // Access the Unit Tests Configuration, which is configured by the Container:
                var configuration = services.GetRequiredService<IConfiguration>();

                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
        }
    }
}