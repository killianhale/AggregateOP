using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AggregateOP.Base;

namespace AggregateOP
{
    public interface IAggregateOrchestrator<TId>
    {
        Task<TId> Change<T>(TId id, long expectedVersion, Action<Dictionary<string, AggregateRoot<TId>>, T> action) where T : AggregateRoot<TId>, new();
        Task<TId> Create<T>(Func<Dictionary<string, AggregateRoot<TId>>, T> action) where T : AggregateRoot<TId>, new();
        IAggregateOrchestrator<TId> FetchDependency<T>(TId id) where T : AggregateRoot<TId>, new();
    }
}