using MediatR;
using SC.Contract.Shared;

namespace SC.Contract.Abstraction.Message;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    where TResponse : notnull
{
}

public interface IQuery : IRequest<Result>;