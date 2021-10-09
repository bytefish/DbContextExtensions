// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DbContextExtensions.Scope
{

    public class DbContextScope<TDbContext> : IDbContextScope<TDbContext>
        where TDbContext : DbContext
    {
        private readonly ILogger<DbContextScope<TDbContext>> logger;

        public DbContextScope<TDbContext>? Parent { get; protected set; }

        public TDbContext? Context { get; protected set; }

        public bool IsReadOnly { get; protected set; }

        public bool AllowSaving { get; protected set; }

        public IsolationLevel IsolationLevel { get; protected set; }

        public bool Failed { get; protected set; }

        public bool Completed { get; protected set; }

        public bool Disposed { get; protected set; }

        public bool DirectSaveFailed { get; protected set; }

        public InvalidOperationException DirectSaveException { get; protected set; }


        public DbContextScope(ILogger<DbContextScope<TDbContext>> logger, IDbContextFactory<TDbContext> dbContextFactory, bool isReadOnly = false, bool allowSaving = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            this.logger = logger;

            var currentStack = AsyncLocalStorage<TDbContext>.GetStack();

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
                Failed = false;
            }
            else
            {
                Context = CreateDbContext(dbContextFactory);
                IsReadOnly = isReadOnly;
                AllowSaving = allowSaving;
                IsolationLevel = isolationLevel;
                Completed = false;
                Disposed = false;
                Failed = false;
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
                DirectSaveFailed = true;
                DirectSaveException = new InvalidOperationException("Don't call SaveChanges directly on a context owned by a DbContextScope. Use DbContextScope#Complete instead or enable AllowSaving on creation.");

                throw DirectSaveException;
            }

            if (AllowSaving && IsReadOnly)
            {
                DirectSaveFailed = true;
                DirectSaveException = new InvalidOperationException("Don't call SaveChanges directly on a context managed by a ReadOnly Scope. Scope the DbContext as Writable and enable AllowSaving to allow direct saves.");

                throw DirectSaveException;
            }
        }

        public TDbContext GetDbContext()
        {
            if (Context == null)
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

            // A DbContextScope can only be completed once. If you attempt to Complete a Scope multiple 
            // times it indicates an error in the application logic and the entire stack should be
            // disposed:
            if (Completed)
            {
                throw new InvalidOperationException("This DbContextScope has been already Completed. Please create a new DbContextScope to save changes.");
            }

            Completed = true;
        }

        private async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (Context == null)
            {
                throw new InvalidOperationException("Cannot Rollback. The DbContext is null. This indicates a severe problem, we cannot recover from.");
            }

            // If the underlying DbContext is still in a Transaction, which should 
            // always be the case for a scoped DbContext, make sure to rollback the 
            // transaction:
            if (Context.Database.CurrentTransaction != null)
            {
                await Context.Database
                    .RollbackTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            // We are not commiting the open Transaction on the DbContextScope, as we are not the outermost scope:
            if (Parent != null)
            {
                return;
            }

            if (Context == null)
            {
                throw new InvalidOperationException("Cannot commit the underlying DbContext. The scoped DbContext is null.");
            }

            // Never call DbContext#SaveChanges on a ReadOnly DbContextScope. The user
            // expects a ReadOnly Scope to never modify the Database, whatever has been 
            // written to the DB yet:
            if (!IsReadOnly)
            {
                await Context
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // If the underlying DbContext is still in a Transaction, which should 
            // always be the case for a scoped DbContext, make sure to commit the 
            // transaction:
            if (Context.Database.CurrentTransaction != null)
            {
                await Context.Database
                    .CommitTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
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
                try
                {
                    // Multiple Dispose calls, shouldn't happen. But they shouldn't throw an error neither:
                    if (Disposed)
                    {
                        logger.LogDebug("The DbContextScope has already been disposed.");

                        return;
                    }

                    Disposed = true;

                    var currentStack = AsyncLocalStorage<TDbContext>.GetStack();

                    if (currentStack.IsEmpty)
                    {
                        throw new InvalidOperationException("Could not dispose scope because it does not exist in storage.");
                    }

                    // Get the top item, if this doesn't match the current scope, someone has disposed out of order:
                    var topItem = currentStack.Peek();

                    if (this != topItem)
                    {
                        throw new InvalidOperationException("Could not dispose scope because it is not the active scope. This could occur because scope is being disposed out of order. This indicates a programming problem.");
                    }

                    // Pop the current scope of the Stack:
                    currentStack = currentStack.Pop();

                    // And save the modified stack, we can do this safely, because we are single-threaded here:
                    AsyncLocalStorage<TDbContext>.SaveStack(currentStack);

                    // The Direct Save failed, we need to throw the original exception here:
                    if (DirectSaveFailed)
                    {
                        if (Parent != null)
                        {
                            // Make sure the parent knows, we had been doing bad, bad stuff when having called SaveChanges inside the scope:
                            Parent.Failed = true;

                            Parent.DirectSaveFailed = true;
                            Parent.DirectSaveException = DirectSaveException;
                        }

                        // Rollback current DbContext transaction, if any:
                        RollbackAsync()
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();

                        throw new InvalidOperationException("Don't call SaveChanges directly on a context owned by a DbContextScope. Use DbContextScope#Complete instead or enable AllowSaving on creation. See the inner Exception for the original Stacktrace.", DirectSaveException);
                    }

                    // Someone might have written a try/catch around a nested scope, which means Exceptions haven't disposed the other scopes 
                    // and someone might still call complete on a parent scope. That's why nested scopes with a parent always set their parent 
                    // as failed, when they fail:
                    if (Failed)
                    {
                        logger.LogDebug("Another Scope has already failed, and there's no way to recover from it.");

                        throw new InvalidOperationException("A nested scope has failed. This indicates a programming problem. Most likely a read/write scope has been disposed without being completed. ");
                    }

                    // If we are presented with a nested DbContextScope, which hasn't been completed at point of being disposed, we
                    // should log, rollback any transaction and throw an Exception here and set the parent scope as failed, so that
                    // all other Scopes are being rolled back too...
                    if (Parent != null && !IsReadOnly && !Completed)
                    {
                        Parent.Failed = true;

                        // Rollback current DbContext transaction:
                        RollbackAsync()
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();

                        throw new InvalidOperationException("A Read/Write DbContextScope has been disposed without calling Complete. The DbContextScope will be aborted and rolled back.");
                    }

                    // If we have started the Transaction, we should commit it, too. If the Scope 
                    // has been completed by the consumer, the scoped DbContext should be saved and 
                    // the wrapping Transaction needs to be commited:
                    if (Parent == null)
                    {
                        // Someone may have nulled the Context. I don't know how this is possible? But we should guard against it anyway:
                        if (Context == null)
                        {
                            throw new InvalidOperationException("Cannot Commit / Rollback, because the Context has already been disposed. This is an error we cannot recover from.");
                        }

                        // If we reach this point and the Guard hasn't been removed
                        // yet, we are removing the Event Handler anyway to prevent
                        // memory leaks:
                        Context.SavingChanges -= GuardAgainstDirectSaves;

                        // If this is a read/write scope and we have been disposed without completing the scope,
                        // this is a programming error and open transactions need to be rolled back.
                        if (!IsReadOnly && !Completed)
                        {
                            // Rollback current DbContext transaction:
                            RollbackAsync()
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();

                            throw new InvalidOperationException("A Read/Write DbContextScope has been disposed without calling Complete. The DbContextScope will be aborted and rolled back.");
                        }

                        if (Context != null)
                        {
                            if (Completed)
                            {
                                logger.LogDebug("The DbContextScope has been completed and is going to be commited.");

                                CommitAsync()
                                    .ConfigureAwait(false)
                                    .GetAwaiter()
                                    .GetResult();
                            }
                            else
                            {
                                logger.LogDebug("The DbContextScope has been disposed. It was the root scope and in read-only mode, so it's safe to rollback any open transaction.");

                                RollbackAsync()
                                    .ConfigureAwait(false)
                                    .GetAwaiter()
                                    .GetResult();
                            }


                        }
                    }
                }
                finally
                {
                    if(Parent == null && Context != null)
                    {
                        // The scoped DbContext is an IDisposable, so make sure to
                        // also dispose it, when disposing the scope:
                        Context.Dispose();

                        Context = null;
                    }
                }
            }
        }


        public async ValueTask DisposeAsync()
        {
            try
            {
                await DisposeAsyncCore();
            }
            finally
            {
                // The scoped DbContext implements an IAsyncDisposable, so make sure to also dispose it,
                // so all its resources will also be disposed:
                if (Parent == null && Context != null)
                {
                    await Context.DisposeAsync();

                    Context = null;
                }
            }

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        public async ValueTask DisposeAsyncCore()
        {
            // Multiple Dispose calls, shouldn't happen. But they shouldn't throw an error neither:
            if (Disposed)
            {
                logger.LogDebug("The DbContextScope has already been disposed.");

                return;
            }

            Disposed = true;

            var currentStack = AsyncLocalStorage<TDbContext>.GetStack();

            if (currentStack.IsEmpty)
            {
                throw new InvalidOperationException("Could not dispose scope because it does not exist in storage.");
            }

            // Get the top item, if this doesn't match the current scope, someone has disposed out of order:
            var topItem = currentStack.Peek();

            if (this != topItem)
            {
                throw new InvalidOperationException("Could not dispose scope because it is not the active scope. This could occur because scope is being disposed out of order. This indicates a programming problem.");
            }

            // Pop the current scope of the Stack:
            currentStack = currentStack.Pop();

            // And save the modified stack, we can do this safely, because we are single-threaded here:
            AsyncLocalStorage<TDbContext>.SaveStack(currentStack);

            // The Direct Save failed, we need to throw the original exception here:
            if (DirectSaveFailed)
            {
                if (Parent != null)
                {
                    // Make sure the parent knows, we had been doing bad, bad stuff when having called SaveChanges inside the scope:
                    Parent.Failed = true;

                    Parent.DirectSaveFailed = true;
                    Parent.DirectSaveException = DirectSaveException;
                }

                // Rollback current DbContext transaction, if any:
                await RollbackAsync().ConfigureAwait(false);

                throw new InvalidOperationException("Don't call SaveChanges directly on a context owned by a DbContextScope. Use DbContextScope#Complete instead or enable AllowSaving on creation. See the inner Exception for the original Stacktrace.", DirectSaveException);
            }

            // Someone might have written a try/catch around a nested scope, which means Exceptions haven't disposed the other scopes 
            // and someone might still call complete on a parent scope. That's why nested scopes with a parent always set their parent 
            // as failed, when they fail:
            if (Failed)
            {
                logger.LogDebug("Another Scope has already failed, and there's no way to recover from it.");

                throw new InvalidOperationException("A nested scope has failed. This indicates a programming problem. Most likely a read/write scope has been disposed without being completed. ");
            }

            // If we are presented with a nested DbContextScope, which hasn't been completed at point of being disposed, we
            // should log, rollback any transaction and throw an Exception here and set the parent scope as failed, so that
            // all other Scopes are being rolled back too...
            if (Parent != null && !IsReadOnly && !Completed)
            {
                Parent.Failed = true;

                // Rollback current DbContext transaction:
                await RollbackAsync().ConfigureAwait(false);

                throw new InvalidOperationException("A Read/Write DbContextScope has been disposed without calling Complete. The DbContextScope will be aborted and rolled back.");
            }

            // If we have started the Transaction, we should commit it, too. If the Scope 
            // has been completed by the consumer, the scoped DbContext should be saved and 
            // the wrapping Transaction needs to be commited:
            if (Parent == null)
            {
                // Someone may have nulled the Context. I don't know how this is possible? But we should guard against it anyway:
                if (Context == null)
                {
                    throw new InvalidOperationException("Cannot Commit / Rollback, because the Context has already been disposed. This is an error we cannot recover from.");
                }

                // If this is a read/write scope and we have been disposed without completing the scope,
                // this is a programming error and open transactions need to be rolled back.
                if (!IsReadOnly && !Completed)
                {
                    // Rollback current DbContext transaction:
                    await RollbackAsync().ConfigureAwait(false);

                    throw new InvalidOperationException("A Read/Write DbContextScope has been disposed without calling Complete. The DbContextScope will be aborted and rolled back.");
                }

                if (Context != null)
                {
                    // If we reach this point and the Guard hasn't been removed
                    // yet, we are removing the Event Handler anyway to prevent
                    // memory leaks:
                    Context.SavingChanges -= GuardAgainstDirectSaves;

                    // Finally we have reached the point we are allowed to save changes and commit:
                    if (Completed)
                    {
                        logger.LogDebug("The DbContextScope has been completed and is going to be commited.");

                        await CommitAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogDebug("The DbContextScope has been disposed. It was the root scope and in read-only mode, so it's safe to rollback any open transaction.");

                        if (IsReadOnly)
                        {
                            await RollbackAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}