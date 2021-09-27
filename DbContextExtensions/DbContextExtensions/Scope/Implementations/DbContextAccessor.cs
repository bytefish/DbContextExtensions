// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Scope
{
    public class DbContextAccessor : IDbContextAccessor
    {
        public TDbContext GetDbContext<TDbContext>()
            where TDbContext : DbContext
        {
            var dbContextScope = GetCurrentScope<TDbContext>();

            return dbContextScope.GetDbContext();
        }

        private DbContextScope<TDbContext> GetCurrentScope<TDbContext>()
            where TDbContext : DbContext
        {
            var immutableStack = AsyncLocalStorage<TDbContext>.GetStack();

            if (immutableStack.IsEmpty)
            {
                throw new Exception("There is no active DbContextScope");
            }

            return immutableStack.Peek();
        }
    }
}
