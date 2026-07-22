using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.User.Manager.ReactivateUser;

public record ReactivateUserCommand(Guid UserId) : ICommand<ReactivateUserResponse>;
