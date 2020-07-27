using System;
using MediatR;

namespace AggregateOP.MediatR
{
    public static class MediatrAggregateOpFactoryExtentions
    {
        public static void AddMediatR<TId>(this AggregateOPFactory<TId> factory)
        {
            factory.SetEventModelType(typeof(MediatedEventModel<>));

            factory._services.AddMediatR(factory._assemblies);
        }
    }
}
