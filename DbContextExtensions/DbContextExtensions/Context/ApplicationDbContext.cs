// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Mappings;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DbContextExtensions.Context
{
    /// <summary>
    /// A base class for a <see cref="DbContext"/> using <see cref="IEntityMap"/> mappings.
    /// </summary>
    public abstract class ApplicationDbContext : DbContext
    {
        private readonly IEnumerable<IEntityMap> mappings;

        /// <summary>
        /// Creates a new <see cref="DbContext"/> to query the database.
        /// </summary>
        /// <param name="loggerFactory">A Logger Factory to enable EF Core Logging facilities</param>
        /// <param name="connection">An opened <see cref="DbConnection"/> to enlist to</param>
        /// <param name="mappings">The <see cref="IEntityMap"/> mappings for mapping query results</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IEnumerable<IEntityMap> mappings)
            : base(options)
        {
            this.mappings = mappings;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var mapping in mappings)
            {
                mapping.Map(modelBuilder);
            }
        }
    }
}
