using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AggregateOP
{
    public static class AggregateOpServiceCollectionExtenstions
    {
        public static void ConfigureAggregateOP(
            this IServiceCollection services,
            Assembly[] assemblies,
            Action<AggregateOPFactory> setup)
        {
            services.AddTransient<IAggregateOrchestrator, AggregateOrchestrator>();

            var factory = new AggregateOPFactory(services, assemblies);

            setup?.Invoke(factory);
        }
    }
}
