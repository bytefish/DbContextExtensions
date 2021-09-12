// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DbContextExtensions.Scope
{
    internal sealed class ContextKey
    {
    }

    internal static class AsyncLocalStorage<TDbContext>
        where TDbContext: DbContext
    {
        private static readonly AsyncLocal<ContextKey> CurrentContextKey = new AsyncLocal<ContextKey>();

        private static readonly ConditionalWeakTable<ContextKey, ImmutableStack<DbContextScope<TDbContext>>> DbContextScope = new ConditionalWeakTable<ContextKey, ImmutableStack<DbContextScope<TDbContext>>>();

        public static void SaveStack(ImmutableStack<DbContextScope<TDbContext>> stack)
        {
            var contextKey = CurrentContextKey.Value;

            if (contextKey == null)
            {
                throw new Exception("No Key found for Scope.");
            }

            if (DbContextScope.TryGetValue(contextKey, out _))
            {
                DbContextScope.Remove(contextKey);
            }

            DbContextScope.Add(contextKey, stack);
        }

        public static ImmutableStack<DbContextScope<TDbContext>> GetStack()
        {
            var contextKey = CurrentContextKey.Value;

            if (contextKey == null)
            {
                contextKey = new ContextKey();

                CurrentContextKey.Value = contextKey;
                DbContextScope.Add(contextKey, ImmutableStack.Create<DbContextScope<TDbContext>>());
            }

            bool keyFound = DbContextScope.TryGetValue(contextKey, out var dbContextScopes);

            if (!keyFound)
            {
                throw new Exception("Stack not found for this DbContextScope");
            }

            return dbContextScopes;
        }
    }
}