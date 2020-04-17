using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AggregateOP.Base
{
    public interface IEventRepository
    {
        Task Save<T>(T aggregate, long expectedVersion = -1, EventMetadata metadata = null) where T : AggregateRoot;
        Task<T> GetAggregateById<T>(Guid id) where T : AggregateRoot, new();
        Task<List<EventModel>> GetAllEventsForAggregateType<T>(long start) where T : AggregateRoot;
        Task<List<EventModel>> GetAllEventsOfType<T>(long start) where T : IEvent;
    }
}
