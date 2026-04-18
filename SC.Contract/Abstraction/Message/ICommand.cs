using MediatR;
using SC.Contract.Shared;

namespace SC.Contract.Abstraction.Message;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;