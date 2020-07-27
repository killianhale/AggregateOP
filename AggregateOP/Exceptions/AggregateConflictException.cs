using System;
using AggregateOP.Base;

namespace AggregateOP.Exceptions
{
    public interface IAggregateConflictException
    {
        string ReasonCode { get; }
    }

    public class AggregateConflictException<TAggregate, TId> : AggregateRootException<TAggregate, TId>, IAggregateConflictException where TAggregate : AggregateRoot<TId>, new()
    {
        private const string reasonCode = "CONFLICT";

        public AggregateConflictException(TId id, string message) : base(id, reasonCode, message)
        {
        }

        public AggregateConflictException(TId id, string message, Exception innerException) : base(id, reasonCode, message, innerException)
        {
        }
    }
}
