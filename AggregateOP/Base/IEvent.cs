using System;

namespace AggregateOP.Base
{
    public interface IEvent<TId>
    {
        TId AggregateId { get; }
    }
}
