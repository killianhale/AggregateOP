using System;
using System.Collections.Generic;

namespace AggregateOP.Base
{
    public abstract class AggregateRoot<TId>
    {
        private readonly string _aggregateTypeID;
        private readonly List<IEvent<TId>> _changes;

        protected IDictionary<Type, Action<IEvent<TId>>> _handlers;
        protected TId _id;

        public abstract TId Id { get; }

        protected AggregateRoot(string aggregateTypeID)
        {
            _aggregateTypeID = aggregateTypeID;

            _changes = new List<IEvent<TId>>();
            _handlers = new Dictionary<Type, Action<IEvent<TId>>>();
        }

        public string GetAggregateTypeID()
        {
            return _aggregateTypeID;
        }

        public IEnumerable<IEvent<TId>> GetUncommittedChanges()
        {
            return _changes;
        }

        public void MarkChangesAsCommitted()
        {
            _changes.Clear();
        }

        public void LoadFromHistory(IEnumerable<EventModel<TId>> history, Action<EventModel<TId>> callback = null)
        {
            foreach (var e in history)
            {
                ApplyChange(e.Event, false);

                callback?.Invoke(e);
            }
        }

        protected void ApplyChange(IEvent<TId> e)
        {
            ApplyChange(e, true);
        }

        private void ApplyChange(IEvent<TId> e, bool isNew)
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
