using System;
using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public class MediatedEventModel : EventModel, IRequest
    {
        public MediatedEventModel(IMediatedEvent e, EventMetadata metadata, long position = -1, long? version = null)
            : base(e, metadata, position, version)
        { }
    }

    public class MediatedEventModel<TEvent> : EventModel<TEvent>, IRequest where TEvent : class, IMediatedEvent
    {
        public MediatedEventModel(TEvent e, EventMetadata metadata, long position = -1, long? version = null)
            : base(e, metadata, position, version)
        {
        }

        public new TEvent Event => _event as TEvent;
    }
}
