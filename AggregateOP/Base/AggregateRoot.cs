using System;
using System.Collections.Generic;

namespace AggregateOP.Base
{
    public abstract class AggregateRoot
    {
        private readonly string _aggregateTypeID;
        private readonly List<IEvent> _changes;

        protected IDictionary<Type, Action<IEvent>> _handlers;
        protected Guid _id;

        public abstract Guid Id { get; }

        protected AggregateRoot(string aggregateTypeID)
        {
            _aggregateTypeID = aggregateTypeID;

            _changes = new List<IEvent>();
            _handlers = new Dictionary<Type, Action<IEvent>>();
        }

        public string GetAggregateTypeID()
        {
            return _aggregateTypeID;
        }

        public IEnumerable<IEvent> GetUncommittedChanges()
        {
            return _changes;
        }

        public void MarkChangesAsCommitted()
        {
            _changes.Clear();
        }

        public void LoadFromHistory(IEnumerable<EventModel> history, Action<EventModel> callback = null)
        {
            foreach (var e in history)
            {
                ApplyChange(e.Event, false);

                callback?.Invoke(e);
            }
        }

        protected void ApplyChange(IEvent e)
        {
            ApplyChange(e, true);
        }

        private void ApplyChange(IEvent e, bool isNew)
        {
            var type = e.GetType();

            if (_handlers.ContainsKey(type))
            {
                _handlers[type](e);
            }

            if (isNew)
            {
                _changes.Add(e);
            }
        }
    }
}
