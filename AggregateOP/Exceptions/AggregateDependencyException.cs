using System;
using AggregateOP.Base;

namespace AggregateOP.Exceptions
{
    public interface IAggregateDependencyException
    {
        string ReasonCode { get; }
    }

    public class AggregateDependencyException<TAggregate, TId> : AggregateRootException<TAggregate, TId>, IAggregateDependencyException where TAggregate : AggregateRoot<TId>, new()
    {
        private const string reasonCode = "DEP";

        public AggregateDependencyException(TId id, string message) : base(id, reasonCode, message)
        {
        }

        public AggregateDependencyException(TId id, string message, Exception innerException) : base(id, reasonCode, message, innerException)
        {
        }
    }
}
