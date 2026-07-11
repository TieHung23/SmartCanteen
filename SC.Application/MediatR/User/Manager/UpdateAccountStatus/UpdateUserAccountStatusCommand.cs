using SC.Contract.Abstraction.Message;
using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.User.Manager.UpdateAccountStatus;

public record UpdateUserAccountStatusCommand(
    Guid UserId,
    AccountStatus Status,
    string Reason) : ICommand<UpdateUserAccountStatusResponse>;
