// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Scope
{
    /// <summary>
    /// Scopes the usage of a <see cref="DbContext"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The <see cref="DbContext"/> type</typeparam>
    public interface IDbContextScope<TDbContext> : IDisposable
        where TDbContext : DbContext
    {
        /// <summary>
        /// Commits the DbContextScope.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Awaitable Task</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the Scoped Transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Awaitable Task</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the underlying <see cref="DbContext"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Currently scoped <see cref="DbContext"/></returns>
        Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default);
    }
}
