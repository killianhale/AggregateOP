using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using ContextRunner.Base;
using AggregateOP.Base;
using ReasonCodeExceptions;

namespace AggregateOP.EventStore
{
    internal class UnprocessedEventData
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string BodyJson { get; set; }
        public string MetaJson { get; set; }
        public long Position { get; set; }
        public long Version { get; set; }
    }

    public class EventRepository : IEventRepository
    {
        private readonly IEventStoreClient _store;
        private readonly string _sourceApp;

        protected IDictionary<string, Func<string, object, long, long, EventModel>> _eventDeserializers;

        public EventRepository(
            IEventStoreClient store,
            IOptionsMonitor<EventStoreConfig> eventStoreOptions,
            IDictionary<string, Func<string, object, long, long, EventModel>> eventDeserializers = null
        )
        {
            _store = store;
            _sourceApp = eventStoreOptions.CurrentValue.ApplicationName;
            _eventDeserializers = eventDeserializers ?? new Dictionary<string, Func<string, object, long, long, EventModel>>();
        }

        #region Read...

        private async Task<List<EventModel>> GetAllEventsFromStream(ActionContext context, IEventStoreConnection connection, string streamName, long start = StreamPosition.Start)
        {
            StreamEventsSlice currentSlice;

            long nextSliceStart = start;
            var sliceSize = 200;

            var data = new List<UnprocessedEventData>();

            do
            {
                context.Logger.Trace($"Getting event slice {nextSliceStart}-{sliceSize} from stream '{streamName}' from the event store.");

                currentSlice = await connection.ReadStreamEventsForwardAsync(streamName, nextSliceStart, sliceSize, true);

                nextSliceStart = currentSlice.NextEventNumber;

                var jsonData = currentSlice.Events.Select(e =>
                {
                    var type = e.Event.EventType;

                    var bodyJson = Encoding.ASCII.GetString(e.Event.Data);

                    var metadataJson = Encoding.ASCII.GetString(e.Event.Metadata);

                    var position = e.IsResolved
                        ? e.Link.EventNumber
                        : e.Event.EventNumber;

                    return new UnprocessedEventData
                    {
                        Id = e.Event.EventId,
                        Type = type,
                        BodyJson = bodyJson,
                        MetaJson = metadataJson,
                        Position = position,
                        Version = e.Event.EventNumber
                    };
                });

                data.AddRange(jsonData);
            } while (!currentSlice.IsEndOfStream);

            if (data.Any())
            {
                context.Logger.Information($"{data.Count()} events found since index {start}!");
            }

            var items = data.Select(e =>
            {
                context.State.SetParam("eventToDeserialize", e);

                if (!_eventDeserializers.ContainsKey(e.Type))
                {
                    throw new InvalidOperationException($"No event deserializer registered for event of type {e.Type}");
                }

                var metaDataObj = JsonConvert.DeserializeObject(e.MetaJson);
                var metaData = EventMetadata.FromObject(metaDataObj);

                var model = _eventDeserializers[e.Type](e.BodyJson, metaData, e.Position, e.Version);

                return model;
            }).ToList();

            context.State.RemoveParam("eventToDeserialize");

            context.State.SetParam("events", items);

            return items.ToList();
        }

        public async Task<List<EventModel>> GetAllEventsForAggregateType<T>(long start = StreamPosition.Start) where T : AggregateRoot
        {
            var results = new List<EventModel>();

            await _store.ConnectWithContext(async (IEventStoreConnection connection, ActionContext context) =>
            {
                var type = typeof(T);
                var streamName = $"$ce-{type.Name}";

                context.Logger.Debug($"Getting all events for Aggregate of type {type.Name} (Stream '{streamName}') starting at position {start}.");

                results = await GetAllEventsFromStream(context, connection, streamName, start);
            }, "GetAllEventsForAggregateType");

            return results;
        }

        public async Task<List<EventModel>> GetAllEventsOfType<T>(long start = StreamPosition.Start) where T : IEvent
        {
            var results = new List<EventModel>();

            await _store.ConnectWithContext(async (IEventStoreConnection connection, ActionContext context) =>
            {
                var type = typeof(T);
                var streamName = $"$et-{type.Name}";

                context.Logger.Debug($"Getting all events for of type {type.Name} (Stream '{streamName}') starting at position {start}.");

                results = await GetAllEventsFromStream(context, connection, streamName, start);
            }, "GetAllEventsOfType");

            return results;
        }

        public async Task<T> GetAggregateById<T>(Guid id) where T : AggregateRoot, new()
        {
            T result = null;

            await _store.ConnectWithContext(async (IEventStoreConnection connection, ActionContext context) =>
            {
                var type = typeof(T);

                context.Logger.Information($"Getting Aggregate of type {type.Name} and ID {id}.");

                var eventsResponse = await GetAllEventsForAggregateType<T>();
                var events = eventsResponse
                    .Where(e => e.Event.AggregateId == id);

                context.Logger.Debug($"Filtered to {events.Count()}/{eventsResponse.Count} events relating to aggregate of type {type.Name} and ID {id}.");

                if (!events.Any())
                {
                    throw new DataNotFoundException($"No events found for aggregate of type {type.Name} and ID {id}");
                }

                context.State.SetParam("events", events);

                context.Logger.Debug($"Applying events to aggregate");

                result = new T();
                result.LoadFromHistory(events, e => context.State.SetParam("currentEvent", e));
                context.State.RemoveParam("currentEvent");

                context.State.SetParam("aggregate", result);
            }, "GetAggregateById");

            return result;
        }

        #endregion

        #region Write...

        public async Task Save<T>(T aggregate, long expectedVersion = -1, EventMetadata metadata = null) where T : AggregateRoot
        {
            await _store.ConnectWithContext(async (IEventStoreConnection connection, ActionContext context) =>
            {
                context.Logger.Information($"Writing changes for Aggregate {aggregate.GetType().Name} ({aggregate.Id}) to the event store.");

                var changes = aggregate.GetUncommittedChanges();

                context.Logger.Trace($"{changes.Count()} changes pending...");

                var serializedChanges = changes.Select(e =>
                {
                    var m = metadata != null
                        ? EventMetadata.FromMetadataTemplate(metadata, expectedVersion)
                        : new EventMetadata(_sourceApp, version: expectedVersion);

                    var meta = JsonConvert.SerializeObject(m);
                    var body = JsonConvert.SerializeObject(e);

                    return new
                    {
                        m.Id,
                        Type = e.GetType().Name,
                        Body = body,
                        Meta = meta
                    };
                });

                context.State.SetParam("changes", serializedChanges);

                var data = serializedChanges.Select(change => new EventData(change.Id, change.Type, true, Encoding.ASCII.GetBytes(change.Body), Encoding.ASCII.GetBytes(change.Meta)));

                var result = await connection.AppendToStreamAsync($"{aggregate.GetType().Name}-{aggregate.Id}", expectedVersion, data);

                context.Logger.Debug("The changes were written to the event store successfully. Clearing the list.");

                aggregate.MarkChangesAsCommitted();
            }, "Save");
        }

        #endregion
    }
}
