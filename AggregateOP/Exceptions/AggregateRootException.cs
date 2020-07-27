using System;
using AggregateOP.Base;
using ReasonCodeExceptions;

namespace AggregateOP.Exceptions
{
    public interface IAggregateRootException
    {
        string ReasonCode { get; }
    }

    public class AggregateRootException<TAggregate, TId> : ReasonCodeException, IAggregateRootException where TAggregate : AggregateRoot<TId>, new()
    {
        private const string reasonCodePrefix = "AGG";

        public AggregateRootException(TId id, string message)
            : base($"{reasonCodePrefix}", message)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        public AggregateRootException(TId id, string message, Exception innerException)
            : base($"{reasonCodePrefix}", message, innerException)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        public AggregateRootException(TId id, string reasonCode, string message)
            : base($"{reasonCodePrefix}{reasonCode}", message)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        public AggregateRootException(TId id, string reasonCode, string message, Exception innerException)
            : base($"{reasonCodePrefix}{reasonCode}", message, innerException)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        private void AddAggregateToReasonCode()
        {
            var typeId = Aggregate?.GetAggregateTypeID() ?? new TAggregate().GetAggregateTypeID();

            ReasonCode = $"{reasonCodePrefix}{typeId}{AggregateId.ToString().ToUpper().Substring(0, 5)}{ReasonCode.Substring(reasonCodePrefix.Length)}";
        }

        public TId AggregateId { get; set; }
        public TAggregate Aggregate { get; set; }
    }
}
