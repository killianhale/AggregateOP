using System;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Guid>
        where TCommand : IMediatedCommand
    {
    }
}
