using System;
using AggregateOP.Base;

namespace AggregateOP.Exceptions
{
    public interface IAggregateConflictException
    {
        string ReasonCode { get; }
    }

    public class AggregateConflictException<T> : AggregateRootException<T>, IAggregateConflictException where T : AggregateRoot, new()
    {
        private const string reasonCode = "CONFLICT";

        public AggregateConflictException(Guid id, string message) : base(id, reasonCode, message)
        {
        }

        public AggregateConflictException(Guid id, string message, Exception innerException) : base(id, reasonCode, message, innerException)
        {
        }
    }
}
