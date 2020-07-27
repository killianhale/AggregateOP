using System;
using AggregateOP.Base;
using MediatR;

namespace AggregateOP.MediatR
{
    public interface IMediatedCommand<TId> : ICommand, IRequest<TId>
    {
    }
}
