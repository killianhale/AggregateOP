using System;

namespace AggregateOP.Base
{
    public class EventModel
    {
        protected IEvent _event;

        public EventModel(IEvent e, EventMetadata metadata, long position = -1, long? version = null)
        {
            _event = e;
            Metadata = metadata;
            Position = position;
            Version = version ?? position;
        }

        public IEvent Event => _event;
        public EventMetadata Metadata { get; protected set; }
        public long Position { get; protected set; }
        public long Version { get; protected set; }
    }

    public class EventModel<TEvent> : EventModel where TEvent : class, IEvent
    {
        public EventModel(TEvent e, EventMetadata metadata, long position = -1, long? version = null)
            : base(e, metadata, position, version)
        {
        }

        public new TEvent Event => _event as TEvent;
    }
}
