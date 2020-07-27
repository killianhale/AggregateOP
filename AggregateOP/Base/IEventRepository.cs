using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AggregateOP.Base
{
    public interface IEventRepository<TId>
    {
        Task Save<T>(T aggregate, long expectedVersion = -1, EventMetadata metadata = null) where T : AggregateRoot<TId>;
        Task<T> GetAggregateById<T>(TId id) where T : AggregateRoot<TId>, new();
        Task<List<EventModel<TId>>> GetAllEventsForAggregateType<T>(long start) where T : AggregateRoot<TId>;
        Task<List<EventModel<TId>>> GetAllEventsOfType<T>(long start) where T : IEvent<TId>;
    }
}
