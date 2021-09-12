// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Scope
{
    internal class DbContextScope<TDbContext> : IDbContextScope<TDbContext>
        where TDbContext : DbContext
    {
        public DbContextScope<TDbContext> Parent { get; protected set; }
        
        public TDbContext Context { get; protected set; }

        public bool IsReadOnly { get; set; }

        public IsolationLevel IsolationLevel { get; protected set; }

        public DbContextScope(IDbContextFactory<TDbContext> dbContextFactory, bool isReadOnly, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var currentStack = AsyncLocalStorage<TDbContext>.GetStack();

            if(!currentStack.IsEmpty)
            {
                var parent = currentStack.Peek();

                if(!parent.IsReadOnly && isReadOnly)
                {
                    throw new InvalidOperationException("Cannot nest a read/write DbContextScope within a read-only DbContextScope.");
                }

                Parent = parent;
                Context = parent.Context;
                IsReadOnly = parent.IsReadOnly;
                IsolationLevel = parent.IsolationLevel;
            }
            else
            {
                Context = CreateDbContext(dbContextFactory);
                IsReadOnly = isReadOnly;
                IsolationLevel = isolationLevel;
            }

            currentStack = currentStack.Push(this);

            AsyncLocalStorage<TDbContext>.SaveStack(currentStack);
        }

        private TDbContext CreateDbContext(IDbContextFactory<TDbContext> dbContextFactory)
        {
            var context = dbContextFactory.CreateDbContext();

            // Guard against direct saves:
            context.SavingChanges += GuardAgainstDirectSaves;

            return context;
        }

        private void GuardAgainstDirectSaves(object sender, SavingChangesEventArgs e)
        {
            throw new InvalidOperationException("Don't call SaveChanges directly on a context owned by a DbContextScope. Use DbContextScope.Commit instead.");
        }

        public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            if(Context.Database.CurrentTransaction == null)
            {
                await Context.Database.BeginTransactionAsync(cancellationToken);
            }

            return Context;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (Parent != null)
            {
                try
                {
                    // Remove Guard against direct saves:
                    Context.SavingChanges -= GuardAgainstDirectSaves;

                    if(!IsReadOnly) 
                    {
                        await Context.SaveChangesAsync(cancellationToken);
                    }

                    await Context.Database.CommitTransactionAsync(cancellationToken);

                }
                catch (Exception)
                {
                    Context.SavingChanges -= GuardAgainstDirectSaves;

                    await Context.Database.RollbackTransactionAsync(cancellationToken);

                    throw;
                }
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if(Parent != null)
            {
                return;
            }

            if (Context.Database.CurrentTransaction != null)
            {
                await Context.Database.RollbackTransactionAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
            var currentStack = AsyncLocalStorage<TDbContext>.GetStack();

            if (currentStack.IsEmpty)
            {
                throw new Exception("Could not dispose scope because it does not exist in storage.");
            }

            var topItem = currentStack.Peek();

            if (this != topItem)
            {
                throw new InvalidOperationException("Could not dispose scope because it is not the active scope. This could occur because scope is being disposed out of order.");
            }

            currentStack = currentStack.Pop();

            AsyncLocalStorage<TDbContext>.SaveStack(currentStack);

            // Cleanup:
            if (Parent == null)
            {
                if (Context != null)
                {
                    Context.SavingChanges -= GuardAgainstDirectSaves;
                    Context.Dispose();

                    Context = null;
                }
            }

            GC.SuppressFinalize(this);
            GC.WaitForPendingFinalizers();
        }
    }
}
