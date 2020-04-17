using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AggregateOP.Base;
using ContextRunner;
using Microsoft.Extensions.Logging;

namespace AggregateOP
{
    public abstract class ProjectionProcessor<T> : IProjectionProcessor where T : AggregateRoot
    {
        private readonly ILogger<ProjectionProcessor<T>> _logger;

        private readonly IContextRunner _runner;
        private readonly IEventRepository _repo;

        private readonly ConcurrentQueue<EventModel> _queue;
        private readonly AutoResetEvent _killEvent;

        private long position;
        private CancellationTokenSource cancellationToken;

        private Thread _queueThread;
        private Thread _processThread;

        private Exception error;

        protected ProjectionProcessor(ILogger<ProjectionProcessor<T>> logger, IEventRepository repo) : this(null, logger, repo) { }

        protected ProjectionProcessor(IContextRunner runner, ILogger<ProjectionProcessor<T>> logger, IEventRepository repo)
        {
            _runner = runner ?? new ContextRunner.ContextRunner();
            _logger = logger;
            _repo = repo;

            _queue = new ConcurrentQueue<EventModel>();
            _killEvent = new AutoResetEvent(false);
        }

        private void Run()
        {
            _queueThread = new Thread(new ThreadStart(Enqueue))
            {
                IsBackground = true
            };

            _processThread = new Thread(new ThreadStart(ProcessQueue))
            {
                IsBackground = true,

            };

            _queueThread.Start();
            _processThread.Start();

            _killEvent.WaitOne();

            Stop();

            if (error != null)
            {
                throw error;
            }
        }

        private void Enqueue()
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _runner.RunAction(context =>
                    {
                        context.Logger.Trace($"Fetching events for aggregate type '{typeof(T).Name}' at position {position}...");

                        var events = _repo.GetAllEventsForAggregateType<T>(position).GetAwaiter().GetResult();

                        if (events.Any())
                        {
                            context.Logger.Information($"Adding {events.Count} events to projection processor queue for aggregate type '{typeof(T).Name}'.");
                        }

                        for (var x = 0; x < events.Count; x++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                context.Logger.Information($"Cancellation Requested! Breaking out of queue for aggregate type '{typeof(T).Name}'.");

                                break;
                            }

                            var e = events[x];

                            context.State.SetParam("eventToQueue", e);

                            position = e.Position + 1;

                            _queue.Enqueue(e);
                        }

                        context.State.RemoveParam("eventToQueue");

                    }, $"{typeof(T).Name}_Enqueue");

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;

                _killEvent.Set();
            }
        }

        private void ProcessQueue()
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _runner.RunAction(context =>
                    {
                        var queueStartedEmpty = _queue.IsEmpty;

                        if (!queueStartedEmpty)
                        {
                            context.Logger.Information($"Processing queue for aggregate type '{typeof(T).Name}'...");
                        }

                        while (!_queue.IsEmpty)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                context.Logger.Information($"Cancellation Requested! Breaking out of queue for aggregate type '{typeof(T).Name}'.");

                                break;
                            }

                            var success = _queue.TryDequeue(out EventModel e);

                            if (success)
                            {
                                context.State.SetParam("eventTypeToProcess", e.Event.GetType().Name);
                                context.State.SetParam("eventToProcess", e);

                                ProcessEvent(e, cancellationToken.Token).GetAwaiter().GetResult();

                                SaveEventPosition(e.Position).GetAwaiter().GetResult();
                            }
                        }

                        context.State.RemoveParam("eventTypeToProcess");
                        context.State.RemoveParam("eventToProcess");

                        if (!queueStartedEmpty)
                        {
                            context.Logger.Debug($"End of queue reached.");
                        }

                    }, $"{typeof(T).Name}Processor_ProcessQueue");

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
                _killEvent.Set();
            }
        }

        protected abstract Task SaveEventPosition(long position);

        protected abstract Task<long> GetStartEventPosition();

        protected abstract Task ProcessEvent(EventModel @event, CancellationToken cancelationToken);

        public void Start()
        {
            _runner.RunAction(context =>
            {
                context.Logger.Information($"Starting projection processor for aggregate type '{typeof(T).Name}'...");

                position = GetStartEventPosition().GetAwaiter().GetResult();

                cancellationToken = new CancellationTokenSource();
            }, $"{typeof(T).Name}Processor_Start");

            Run();
        }

        public void Stop()
        {
            _logger.LogInformation($"Stopping projection processor for aggregate type '{typeof(T).Name}'...");

            cancellationToken.Cancel();
        }
    }
}
