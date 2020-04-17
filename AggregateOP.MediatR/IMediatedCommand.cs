using System;
using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface IMediatedCommand : ICommand, IRequest<Guid>
    {
    }
}
