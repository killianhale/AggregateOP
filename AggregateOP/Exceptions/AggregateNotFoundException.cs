using System;
using AggregateOP.Base;

namespace AggregateOP.Exceptions
{
    public interface IAggregateNotFoundException
    {
        string ReasonCode { get; }
    }

    public class AggregateNotFoundException<T> : AggregateRootException<T>, IAggregateNotFoundException where T : AggregateRoot, new()
    {
        private const string reasonCode = "NF";

        public AggregateNotFoundException(Guid id, string message) : base(id, reasonCode, message)
        {
        }

        public AggregateNotFoundException(Guid id, string message, Exception innerException) : base(id, reasonCode, message, innerException)
        {
        }
    }
}
