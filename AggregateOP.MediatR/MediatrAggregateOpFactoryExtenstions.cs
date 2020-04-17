using System;
using MediatR;

namespace AggregateOP.MediatR
{
    public static class MediatrAggregateOpFactoryExtentions
    {
        public static void AddMediatR(this AggregateOPFactory factory)
        {
            factory.SetEventModelType(typeof(MediatedEventModel<>));

            factory._services.AddMediatR(factory._assemblies);
        }
    }
}
