using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.GetCurrentUser;

public record GetCurrentUserQuery() : IQuery<UserProfileResponse>;
