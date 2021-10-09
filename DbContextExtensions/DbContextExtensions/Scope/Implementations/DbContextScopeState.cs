// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DbContextExtensions.Scope
{
    /// <summary>
    /// Holds the overall DbContextScope Status.
    /// </summary>
    internal class DbContextScopeState
    {
        public enum StatusEnum
        {
            /// <summary>
            /// The DbContextScope is active.
            /// </summary>
            Active = 0,

            /// <summary>
            /// The DbContextScope has been commited.
            /// </summary>
            Commited = 1,

            /// <summary>
            /// The DbContextScope has been aborted due to an exception.
            /// </summary>
            Aborted = 2
        }

        public readonly StatusEnum Status;

        public DbContextScopeState(StatusEnum status)
        {
            Status = status;
        }
    }
}