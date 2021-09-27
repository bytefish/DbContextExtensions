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

            // No one has set the AsyncLocal<...> yet, so create a new ContextKey 
            // for lookups in the ConditionalWeakTable and set the CurrentContextKey:
            if (contextKey == null)
            {
                contextKey = new ContextKey();

                CurrentContextKey.Value = contextKey;
            }

            // Try to get the current DbContextScope atomically, if there is 
            // no entry yet create a new (empty) ImmutableStack:
            return DbContextScope.GetValue(contextKey, createValueCallback =>
            {
                return ImmutableStack.Create<DbContextScope<TDbContext>>();
            });
        }
    }
}