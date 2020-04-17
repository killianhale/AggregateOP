using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using ContextRunner.Base;

namespace AggregateOP.EventStore
{
    public interface IEventStoreClient
    {
        Task ConnectWithContext(Func<IEventStoreConnection, ActionContext, Task> action, string contextSubName = null);
    }
}