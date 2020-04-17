using System;
using AggregateOP.Base;
using ReasonCodeExceptions;

namespace AggregateOP.Exceptions
{
    public interface IAggregateRootException
    {
        string ReasonCode { get; }
    }

    public class AggregateRootException<T> : ReasonCodeException, IAggregateRootException where T : AggregateRoot, new()
    {
        private const string reasonCodePrefix = "AGG";

        public AggregateRootException(Guid id, string message)
            : base($"{reasonCodePrefix}", message)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        public AggregateRootException(Guid id, string message, Exception innerException)
            : base($"{reasonCodePrefix}", message, innerException)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        public AggregateRootException(Guid id, string reasonCode, string message)
            : base($"{reasonCodePrefix}{reasonCode}", message)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        public AggregateRootException(Guid id, string reasonCode, string message, Exception innerException)
            : base($"{reasonCodePrefix}{reasonCode}", message, innerException)
        {
            AggregateId = id;

            AddAggregateToReasonCode();
        }

        private void AddAggregateToReasonCode()
        {
            var typeId = Aggregate?.GetAggregateTypeID() ?? new T().GetAggregateTypeID();

            ReasonCode = $"{reasonCodePrefix}{typeId}{AggregateId.ToString().ToUpper().Substring(0, 5)}{ReasonCode.Substring(reasonCodePrefix.Length)}";
        }

        public Guid AggregateId { get; set; }
        public T Aggregate { get; set; }
    }
}
