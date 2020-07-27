using System;
using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface IMediatedEvent<TId> : IEvent<TId>, IRequest<TId>
    {
    }
}
