using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContextRunner;
using AggregateOP.Exceptions;
using AggregateOP.Base;
using ReasonCodeExceptions;

namespace AggregateOP
{
    public class AggregateOrchestrator : IAggregateOrchestrator
    {
        private readonly IEventRepository _repo;
        private readonly IContextRunner _runner;

        private Dictionary<string, AggregateRoot> _dependencies;

        public AggregateOrchestrator(IEventRepository repo, IContextRunner runner)
        {
            _repo = repo;
            _runner = runner;

            _dependencies = new Dictionary<string, AggregateRoot>();
        }

        public AggregateOrchestrator(IEventRepository repo, Dictionary<string, AggregateRoot> dependencies)
        {
            _repo = repo;

            _dependencies = dependencies ?? new Dictionary<string, AggregateRoot>();
        }
        public IAggregateOrchestrator FetchDependency<T>(Guid id) where T : AggregateRoot, new()
        {
            return FetchDependencyAsync<T>(id).GetAwaiter().GetResult();
        }

        public async Task<IAggregateOrchestrator> FetchDependencyAsync<T>(Guid id) where T : AggregateRoot, new()
        {
            var aggregateType = typeof(T).Name;

            return await _runner.RunAction(async context =>
            {
                try
                {
                    context.Logger.Debug($"Fetching aggregate dependency of type {aggregateType} and ID {id}.");

                    var aggregate = await _repo.GetAggregateById<T>(id);

                    _dependencies.Add($"{aggregate.GetAggregateTypeID()}-{id}", aggregate);

                    context.State.SetParam("aggregateDependencies", _dependencies);
                }
                catch (Exception ex)
                {
                    if (ex is DataNotFoundException)
                    {
                        throw LogAndReturnException(context.Logger.Warning, new AggregateDependencyException<T>(id, $"Unable to find the aggregate with type {aggregateType} and ID {id}", ex));
                    }
                    else
                    {
                        throw ex;
                    }
                }

                return new AggregateOrchestrator(_repo, _dependencies);

            }, nameof(AggregateOrchestrator));
        }

        public async Task<Guid> Change<T>(Guid id, long expectedVersion, Action<Dictionary<string, AggregateRoot>, T> action) where T : AggregateRoot, new()
        {
            var aggregateType = typeof(T).Name;

            return await _runner.RunAction((Func<ContextRunner.Base.ActionContext, Task<Guid>>)(async context =>
            {
                T aggregate = null;

                try
                {
                    context.Logger.Debug($"Changing aggregate of type {aggregateType} and ID {id}.");

                    aggregate = await _repo.GetAggregateById<T>(id);

                    action?.Invoke(_dependencies, aggregate);

                    await _repo.Save(aggregate, expectedVersion);

                    return aggregate.Id;
                }
                catch (Exception ex)
                {
                    if (ex is DataNotFoundException)
                    {
                        throw LogAndReturnException(context.Logger.Warning, new AggregateNotFoundException<T>(id, $"Unable to find the aggregate {aggregateType} {id}", (Exception)ex));
                    }
                    else if (ex is DataConflictException)
                    {
                        throw LogAndReturnException(context.Logger.Warning, new AggregateConflictException<T>(id, $"Unable to change aggregate {aggregateType} {id} because it's out of date.", (Exception)ex)
                        {
                            Aggregate = aggregate
                        });
                    }
                    else if (ex is ArgumentException || ex is InvalidOperationException)
                    {
                        throw LogAndReturnException(context.Logger.Warning, new AggregateRootException<T>(id, $"Unable to change aggregate {aggregateType} {id}. {ex.Message}", (Exception)ex)
                        {
                            Aggregate = aggregate
                        });
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }), nameof(AggregateOP.AggregateOrchestrator));
        }

        public async Task<Guid> Create<T>(Func<Dictionary<string, AggregateRoot>, T> action) where T : AggregateRoot, new()
        {
            var aggregateType = typeof(T).Name;

            return await _runner.RunAction((Func<ContextRunner.Base.ActionContext, Task<Guid>>)(async context =>
            {
                T aggregate = null;

                try
                {
                    context.Logger.Debug($"Creating new aggregate of type {aggregateType}.");

                    aggregate = action?.Invoke(_dependencies);

                    if (aggregate == null)
                    {
                        throw new Exception($"Unable to create aggregate {aggregateType}. Delegate resulted in null.");
                    }

                    await _repo.Save(aggregate, -1);

                    return aggregate.Id;
                }
                catch (Exception ex)
                {
                    if (ex is DataConflictException)
                    {
                        throw LogAndReturnException(context.Logger.Warning,
                            new AggregateConflictException<T>(aggregate.Id, $"Unable to create aggregate {aggregateType} {aggregate.Id}. An aggregate with that type and ID already exists!", (Exception)ex));
                    }
                    else if (ex is ArgumentException || ex is InvalidOperationException)
                    {
                        throw LogAndReturnException(context.Logger.Warning,
                            new AggregateRootException<T>(aggregate.Id, $"Unable to create aggregate {aggregateType} {aggregate.Id}. {ex.Message}", (Exception)ex));
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }), nameof(AggregateOP.AggregateOrchestrator));
        }

        private Exception LogAndReturnException(Action<string, bool> logMethod, Exception ex)
        {
            logMethod?.Invoke(ex.Message, false);

            return ex;
        }
    }
}
