using System;
using System.Linq;
using System.Reflection;
using AggregateOP.Base;
using Microsoft.Extensions.DependencyInjection;

namespace AggregateOP
{
    public class AggregateOPFactory<TId>
    {
        public readonly IServiceCollection _services;
        public readonly Assembly[] _assemblies;

        public AggregateOPFactory(IServiceCollection services, Assembly[] assemblies)
        {
            _services = services;
            _assemblies = assemblies;

            SetEventModelType(typeof(EventModel<>));
        }

        public Type EventModelType { get; private set; }

        public void AddEventRepository<T>() where T : class, IEventRepository<TId>
        {
            _services.AddSingleton<IEventRepository<TId>, T>();
        }

        public void AddEventRepository<T>(Func<IServiceProvider, T> factory) where T : class, IEventRepository<TId>
        {
            _services.AddSingleton<IEventRepository<TId>, T>(factory);
        }

        public void AddCommandHandlers()
        {
            _assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.Name.EndsWith("CommandHandler"))
                .ToList()
                .ForEach(t => _services.AddScoped(t));
        }

        public void AddEventHandlers()
        {
            _assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.Name.EndsWith("EventHandler"))
                .ToList()
                .ForEach(t => _services.AddScoped(t));
        }

        public void SetEventModelType(Type type)
        {
            if (!typeof(EventModel<>).IsAssignableFrom(typeof(EventModel<>)))
            {
                throw new ArgumentException("The type specified does not implement EventModel!");
            }

            EventModelType = type;
        }
    }
}
