using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContextRunner;
using AggregateOP.Exceptions;
using AggregateOP.Base;
using ReasonCodeExceptions;

namespace AggregateOP
{
    public class AggregateOrchestrator<TId> : IAggregateOrchestrator<TId>
    {
        private readonly IEventRepository<TId> _repo;
        private readonly IContextRunner _runner;

        private Dictionary<string, AggregateRoot<TId>> _dependencies;

        public AggregateOrchestrator(IEventRepository<TId> repo, IContextRunner runner)
        {
            _repo = repo;
            _runner = runner;

            _dependencies = new Dictionary<string, AggregateRoot<TId>>();
        }

        public AggregateOrchestrator(IEventRepository<TId> repo, Dictionary<string, AggregateRoot<TId>> dependencies)
        {
            _repo = repo;

            _dependencies = dependencies ?? new Dictionary<string, AggregateRoot<TId>>();
        }
        public IAggregateOrchestrator<TId> FetchDependency<T>(TId id) where T : AggregateRoot<TId>, new()
        {
            return FetchDependencyAsync<T>(id).GetAwaiter().GetResult();
        }

        public async Task<IAggregateOrchestrator<TId>> FetchDependencyAsync<T>(TId id) where T : AggregateRoot<TId>, new()
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
                        throw LogAndReturnException(context.Logger.Warning, new AggregateDependencyException<T, TId>(id, $"Unable to find the aggregate with type {aggregateType} and ID {id}", ex));
                    }
                    else
                    {
                        throw ex;
                    }
                }

                return new AggregateOrchestrator<TId>(_repo, _dependencies);

            }, nameof(AggregateOrchestrator<TId>));
        }

        public async Task<TId> Change<T>(TId id, long expectedVersion, Action<Dictionary<string, AggregateRoot<TId>>, T> action) where T : AggregateRoot<TId>, new()
        {
            var aggregateType = typeof(T).Name;

            return await _runner.RunAction((Func<ContextRunner.Base.ActionContext, Task<TId>>)(async context =>
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
                        throw LogAndReturnException(context.Logger.Warning, new AggregateNotFoundException<T, TId>(id, $"Unable to find the aggregate {aggregateType} {id}", (Exception)ex));
                    }
                    else if (ex is DataConflictException)
                    {
                        throw LogAndReturnException(context.Logger.Warning, new AggregateConflictException<T, TId>(id, $"Unable to change aggregate {aggregateType} {id} because it's out of date.", (Exception)ex)
                        {
                            Aggregate = aggregate
                        });
                    }
                    else if (ex is ArgumentException || ex is InvalidOperationException)
                    {
                        throw LogAndReturnException(context.Logger.Warning, new AggregateRootException<T, TId>(id, $"Unable to change aggregate {aggregateType} {id}. {ex.Message}", (Exception)ex)
                        {
                            Aggregate = aggregate
                        });
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }), nameof(AggregateOP.AggregateOrchestrator<TId>));
        }

        public async Task<TId> Create<T>(Func<Dictionary<string, AggregateRoot<TId>>, T> action) where T : AggregateRoot<TId>, new()
        {
            var aggregateType = typeof(T).Name;

            return await _runner.RunAction((Func<ContextRunner.Base.ActionContext, Task<TId>>)(async context =>
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
                            new AggregateConflictException<T, TId>(aggregate.Id, $"Unable to create aggregate {aggregateType} {aggregate.Id}. An aggregate with that type and ID already exists!", (Exception)ex));
                    }
                    else if (ex is ArgumentException || ex is InvalidOperationException)
                    {
                        throw LogAndReturnException(context.Logger.Warning,
                            new AggregateRootException<T, TId>(aggregate.Id, $"Unable to create aggregate {aggregateType} {aggregate.Id}. {ex.Message}", (Exception)ex));
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }), nameof(AggregateOP.AggregateOrchestrator<TId>));
        }

        private Exception LogAndReturnException(Action<string, bool> logMethod, Exception ex)
        {
            logMethod?.Invoke(ex.Message, false);

            return ex;
        }
    }
}
