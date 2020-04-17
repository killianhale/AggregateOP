using System;

namespace AggregateOP.Base
{
    public interface IEvent
    {
        Guid AggregateId { get; }
    }
}
