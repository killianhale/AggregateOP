using System;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface ICommandHandler<TCommand, TId> : IRequestHandler<TCommand, TId>
        where TCommand : IMediatedCommand<TId>
    {
    }
}
