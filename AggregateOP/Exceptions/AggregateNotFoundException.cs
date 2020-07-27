using System;
using AggregateOP.Base;

namespace AggregateOP.Exceptions
{
    public interface IAggregateNotFoundException
    {
        string ReasonCode { get; }
    }

    public class AggregateNotFoundException<TAggregate, TId> : AggregateRootException<TAggregate, TId>, IAggregateNotFoundException where TAggregate : AggregateRoot<TId>, new()
    {
        private const string reasonCode = "NF";

        public AggregateNotFoundException(TId id, string message) : base(id, reasonCode, message)
        {
        }

        public AggregateNotFoundException(TId id, string message, Exception innerException) : base(id, reasonCode, message, innerException)
        {
        }
    }
}
