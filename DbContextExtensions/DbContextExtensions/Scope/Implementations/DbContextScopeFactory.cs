// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DbContextExtensions.Scope
{
    /// <summary>
    /// A <see cref="IDbContextScopeFactory{TDbContext}"/> is used to create DbContextScopes.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext to be scoped</typeparam>
    public class DbContextScopeFactory<TDbContext> : IDbContextScopeFactory<TDbContext>
        where TDbContext : DbContext
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IDbContextFactory<TDbContext> dbContextFactory;
        
        public DbContextScopeFactory(ILoggerFactory loggerFactory, IDbContextFactory<TDbContext> dbContextFactory)
        {
            this.loggerFactory = loggerFactory;
            this.dbContextFactory = dbContextFactory;
        }

        public IDbContextScope<TDbContext> Create(bool isReadOnly = false, bool allowSaving = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var logger = loggerFactory.CreateLogger<DbContextScope<TDbContext>>();

            return new DbContextScope<TDbContext>(logger, dbContextFactory, isReadOnly, allowSaving, isolationLevel);
        }
    }
}