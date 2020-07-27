using System;
using AggregateOP.Base;
using Newtonsoft.Json;

namespace AggregateOP.EventStore.Deserializers
{
    public static class JsonEventDeserializer<TEvent, TId> where TEvent : IEvent<TId>
    {
        public static TEvent Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<TEvent>(json, new JsonSerializerSettings()
            {
                MaxDepth = 2,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatString = "YYYY-MM-DDTHH:mm:ss.sssZ"
            });
        }
    }
}
