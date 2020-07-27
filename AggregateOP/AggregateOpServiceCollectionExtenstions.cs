using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AggregateOP
{
    public static class AggregateOpServiceCollectionExtenstions
    {
        public static void ConfigureAggregateOP<TId>(
            this IServiceCollection services,
            Assembly[] assemblies,
            Action<AggregateOPFactory<TId>> setup)
        {
            services.AddTransient<IAggregateOrchestrator<TId>, AggregateOrchestrator<TId>>();

            var factory = new AggregateOPFactory<TId>(services, assemblies);

            setup?.Invoke(factory);
        }
    }
}
