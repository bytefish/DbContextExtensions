// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DbContextExtensions.Scope
{
    /// <summary>
    /// A Factory to create a <see cref="IDbContextScope{TDbContext}"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The <see cref="DbContext"/> type</typeparam>
    public interface IDbContextScopeFactory<TDbContext>
        where TDbContext : DbContext
    {
        /// <summary>
        /// Creates a new <see cref="IDbContextScope{TDbContext}"/> with the given Isolation Level.
        /// </summary>
        /// <param name="isReadOnly">A read-only <see cref="IDbContextScope{TDbContext}"/> prevents writes in a Scope</param>
        /// <param name="isolationLevel">The Isolation Level of the underlying Transaction in a Scope</param>
        /// <returns>A scoped <see cref="DbContext"/></returns>
        IDbContextScope<TDbContext> Create(bool isReadOnly = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }
}
