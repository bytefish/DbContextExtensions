using System;
using System.Runtime.Serialization;

namespace DbContextExtensions.Exceptions
{
    [Serializable]
    public class DbContextScopeAbortedException : Exception
    {
        public DbContextScopeAbortedException()
            : base() { }

        public DbContextScopeAbortedException(string? message)
            : base(message) { }

        public DbContextScopeAbortedException(string? message, Exception? innerException)
            : base(message, innerException) { }

        public DbContextScopeAbortedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}
