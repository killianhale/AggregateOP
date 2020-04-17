using System;
using System.Threading;
using System.Threading.Tasks;
using ContextRunner;
using AggregateOP.Base;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AggregateOP.MediatR
{
    public abstract class MediatedProjectionProcessor<T> : ProjectionProcessor<T> where T : AggregateRoot
    {
        private readonly IMediator _mediator;

        protected MediatedProjectionProcessor(ILogger<ProjectionProcessor<T>> logger, IEventRepository repo, IMediator mediator) : this(null, logger, repo, mediator) { }

        protected MediatedProjectionProcessor(IContextRunner runner, ILogger<ProjectionProcessor<T>> logger, IEventRepository repo, IMediator mediator) : base(runner, logger, repo)
        {
            _mediator = mediator;
        }

        protected override async Task ProcessEvent(EventModel @event, CancellationToken cancelationToken)
        {
            await _mediator.Send(@event, cancelationToken);
        }
    }
}
