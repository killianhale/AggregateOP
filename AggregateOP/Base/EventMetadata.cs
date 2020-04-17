using System;
namespace AggregateOP.Base
{
    public class EventMetadata
    {
        public static EventMetadata FromCausingEvent(EventMetadata eventData, long version = -1)
        {
            return new EventMetadata(eventData.SourceApplication, null, eventData.CorrelationId, eventData.Id, version);
        }

        public static EventMetadata FromObject(dynamic eventData)
        {
            var sourceApp = eventData.SourceApplication?.ToString();
            var id = eventData.Id != null ? Guid.Parse(eventData.Id.ToString()) : null;
            var correlationId = eventData.CorrelationId != null ? Guid.Parse(eventData.CorrelationId.ToString()) : null;
            var causationId = eventData.CausationId != null ? Guid.Parse(eventData.CausationId.ToString()) : null;
            var version = eventData.Version != null ? Convert.ToInt64(eventData.Version) : null;

            return new EventMetadata(sourceApp, id, correlationId, causationId, version);
        }

        public static EventMetadata FromMetadataTemplate(EventMetadata eventData, long? version = null)
        {
            var v = version ?? eventData.Version;

            return new EventMetadata(eventData.SourceApplication, null, eventData.CorrelationId, eventData.CausationId, v);
        }

        public EventMetadata(string sourceApplication, Guid? id = null, Guid? correlationId = null, Guid? causationId = null, long version = -1)
        {
            Id = id ?? Guid.NewGuid();
            CorrelationId = correlationId ?? Id;
            CausationId = causationId ?? Id;

            Version = version;
            SourceApplication = sourceApplication;
        }

        public Guid Id { get; private set; }
        public Guid CorrelationId { get; private set; }
        public Guid CausationId { get; private set; }
        public long Version { get; private set; }
        public string SourceApplication { get; private set; }
    }
}
