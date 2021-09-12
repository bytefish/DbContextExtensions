// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DbContextExtensions.Scope
{
    public class DbContextScopeFactory<TDbContext> : IDbContextScopeFactory<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IDbContextFactory<TDbContext> dbContextFactory;

        public DbContextScopeFactory(IDbContextFactory<TDbContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
        }

        public IDbContextScope<TDbContext> Create(bool isReadOnly = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return new DbContextScope<TDbContext>(dbContextFactory, isReadOnly, isolationLevel);
        }
    }
}
