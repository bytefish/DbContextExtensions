// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Scope
{
    /// <summary>
    /// An <see cref="IDbContextAccessor"/> resolves the current <see cref="DbContext"/>.
    /// </summary>
    public interface IDbContextAccessor
    {
        /// <summary>
        /// Gets the <see cref="DbContext"/> of the current scope.
        /// </summary>
        /// <typeparam name="TDbContext">The <see cref="DbContext"/> type</typeparam>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>The currently scoped <see cref="DbContext"/></returns>
        TDbContext GetDbContext<TDbContext>()
            where TDbContext : DbContext;
    }
}
