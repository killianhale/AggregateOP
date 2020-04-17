using System;
using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface IMediatedEvent : IEvent, IRequest<Guid>
    {
    }
}
