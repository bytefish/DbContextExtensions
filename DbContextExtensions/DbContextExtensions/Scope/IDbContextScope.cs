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
    public interface IDbContextScope<TDbContext> : IDisposable, IAsyncDisposable
        where TDbContext : DbContext
    {
        /// <summary>
        /// Completes the current DbContextScope.
        /// </summary>
        void Complete();

        /// <summary>
        /// Gets the underlying <see cref="DbContext"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Currently scoped <see cref="DbContext"/></returns>
        TDbContext GetDbContext();
    }
}
