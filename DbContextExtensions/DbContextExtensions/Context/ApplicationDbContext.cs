// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DbContextExtensions.Context
{
    /// <summary>
    /// A base class for a <see cref="DbContext"/> using <see cref="IEntityMap"/> mappings.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        private readonly IReadOnlyCollection<IEntityMap> mappings;

        /// <summary>
        /// Creates a new <see cref="DbContext"/> to query the database.
        /// </summary>
        /// <param name="loggerFactory">A Logger Factory to enable EF Core Logging facilities</param>
        /// <param name="mappings">The <see cref="IEntityMap"/> mappings for mapping query results</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IEnumerable<IEntityMap> mappings)
            : base(options)
        {
            this.mappings = mappings
                .ToList()
                .AsReadOnly();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ApplyMappings(modelBuilder);
        }

        private void ApplyMappings(ModelBuilder modelBuilder)
        {
            foreach (var mapping in mappings)
            {
                Logger.LogDebug("Applying EntityMap {EntityMap}", mapping.GetType());

                mapping.Map(modelBuilder);
            }
        }

        protected ILogger<ApplicationDbContext> Logger => this.GetService<ILogger<ApplicationDbContext>>();
    }
}