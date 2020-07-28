using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface IEventHandler<TEvent, TId> : IRequestHandler<MediatedEventModel<TEvent, TId>>
        where TEvent : class, IMediatedEvent<TId>
    {
    }
}
