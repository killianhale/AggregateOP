using System;
using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public class MediatedEventModel<TId> : EventModel<TId>, IRequest
    {
        public MediatedEventModel(IMediatedEvent<TId> e, EventMetadata metadata, long position = -1, long? version = null)
            : base(e, metadata, position, version)
        { }
    }

    public class MediatedEventModel<TEvent, TId> : EventModel<TEvent, TId>, IRequest where TEvent : class, IMediatedEvent<TId>
    {
        public MediatedEventModel(TEvent e, EventMetadata metadata, long position = -1, long? version = null)
            : base(e, metadata, position, version)
        {
        }

        public new TEvent Event => _event as TEvent;
    }
}
