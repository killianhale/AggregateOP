using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface IEventHandler<TEvent> : IRequestHandler<MediatedEventModel<TEvent>>
        where TEvent : class, IMediatedEvent
    {
    }
}
