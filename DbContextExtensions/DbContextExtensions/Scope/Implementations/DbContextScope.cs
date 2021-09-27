// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Scope
{
    public class DbContextScope<TDbContext> : IDbContextScope<TDbContext>
        where TDbContext : DbContext
    {
        public DbContextScope<TDbContext>? Parent { get; protected set; }

        public TDbContext? Context { get; protected set; }

        public bool IsReadOnly { get; set; }

        public bool AllowSaving { get; set; }

        public IsolationLevel IsolationLevel { get; protected set; }

        public bool Completed { get; protected set; }

        public bool Disposed { get; private set; }

        public DbContextScope(IDbContextFactory<TDbContext> dbContextFactory, bool join = true, bool isReadOnly = false, bool allowSaving = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var currentStack = AsyncLocalStorage<TDbContext>.GetStack();

            if (join)
            {

                if (!currentStack.IsEmpty)
                {
                    var parent = currentStack.Peek();

                    if (parent.IsReadOnly && !isReadOnly)
                    {
                        throw new InvalidOperationException("Cannot nest a read/write DbContextScope within a read-only DbContextScope.");
                    }

                    Parent = parent;
                    Context = parent.Context;
                    IsReadOnly = parent.IsReadOnly;
                    IsolationLevel = parent.IsolationLevel;
                    AllowSaving = parent.AllowSaving;
                    Completed = false;
                    Disposed = false;
                }
                else
                {
                    Context = CreateDbContext(dbContextFactory);
                    IsReadOnly = isReadOnly;
                    AllowSaving = allowSaving;
                    IsolationLevel = isolationLevel;
                    Completed = false;
                    Disposed = false;
                }
            }
            else
            {
                Context = CreateDbContext(dbContextFactory);
                IsReadOnly = isReadOnly;
                AllowSaving = allowSaving;
                IsolationLevel = isolationLevel;
                Completed = false;
                Disposed = false;
            }

            currentStack = currentStack.Push(this);

            AsyncLocalStorage<TDbContext>.SaveStack(currentStack);
        }

        private TDbContext CreateDbContext(IDbContextFactory<TDbContext> dbContextFactory)
        {
            var context = dbContextFactory.CreateDbContext();

            // Guard against direct saves:
            context.SavingChanges += GuardAgainstDirectSaves;

            // Always wrap in a Transaction, if there isn't any yet:
            context.Database.BeginTransaction();

            return context;
        }

        private void GuardAgainstDirectSaves(object? sender, SavingChangesEventArgs e)
        {
            if (!AllowSaving)
            {
                throw new InvalidOperationException("Don't call SaveChanges directly on a context owned by a DbContextScope. Use DbContextScope.Commit instead or enable AllowSaving on creation.");
            }
        }

        public TDbContext GetDbContext()
        {
            if(Context == null)
            {
                throw new InvalidOperationException("Cannot get underlying DbContext");
            }

            return Context;
        }

        public void Complete()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(DbContextScope<TDbContext>));
            }

            if (Completed)
            {
                throw new DbContextScopeAbortedException("Could not complete more than once");
            }

            Completed = true;
        }

        private async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(DbContextScope<TDbContext>));
            }

            if (Context == null)
            {
                throw new InvalidOperationException("Cannot Rollback underlying DbContext. DbContext is null.");
            }

            // We should always rollback:
            Context.SavingChanges -= GuardAgainstDirectSaves;

            if (Context.Database.CurrentTransaction != null)
            {
                await Context.Database
                    .RollbackTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            // We are not commiting the open Transaction on the DbContextScope, as we are not the outermost scope:
            if (Parent != null)
            {
                return;
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(DbContextScope<TDbContext>));
            }

            if (Context == null)
            {
                throw new InvalidOperationException("Cannot commit the underlying DbContext. DbContext is null.");
            }

            // Remove Guard against direct saves:
            Context.SavingChanges -= GuardAgainstDirectSaves;

            if (!IsReadOnly)
            {
                await Context
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            await Context.Database
                .CommitTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
        }



        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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

                // If we are presented with a nested DbContextScope, which hasn't been completed
                // at the point of being disposed, we should throw an Exception here and make sure, 
                // that all other Scopes are being disposed too...
                if(Parent != null && !IsReadOnly && !Completed)
                {
                    throw new DbContextScopeAbortedException();
                }

                // Cleanup:
                if (Parent == null)
                {
                    if (Context != null)
                    {
                        // Remove Guard against direct saves:
                        Context.SavingChanges -= GuardAgainstDirectSaves;

                        if(Completed)
                        {
                            if (Context.Database.CurrentTransaction != null)
                            {
                                CommitAsync()
                                    .GetAwaiter()
                                    .GetResult();
                            }
                        } else
                        {
                            RollbackAsync()
                                .GetAwaiter()
                                .GetResult();
                        }

                        // Then dispose the Context:
                        Context.Dispose();

                        Context = null;
                    }
                }

                // Mark this instance as disposed:
                Disposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore()
                .ConfigureAwait(false);

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        protected virtual async ValueTask DisposeAsyncCore()
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

            // If we are presented with a nested DbContextScope, which hasn't been completed
            // at the point of being disposed, we should throw an Exception here and make sure, 
            // that all other Scopes are being disposed too...
            if (Parent != null && !IsReadOnly && !Completed)
            {
                throw new DbContextScopeAbortedException();
            }

            if (Parent == null)
            {
                if (Context != null)
                {
                    // Remove Guard against direct saves:
                    Context.SavingChanges -= GuardAgainstDirectSaves;

                    if (Completed)
                    {
                        if (Context.Database.CurrentTransaction != null)
                        {
                            await CommitAsync();
                        }
                    }
                    else
                    {
                        await RollbackAsync() ;
                    }

                    // Then dispose the Context:
                    Context.Dispose();

                    Context = null;
                }
            }

            // Mark this instance as disposed:
            Disposed = true;
        }

    }
}