namespace SC.Domain.Domain.User.Enum;

public enum AccountStatus
{
    Active = 1,
    PendingEmailVerification = 2,
    PendingIdentityVerification = 3,
    Suspended = 4,
    Banned = 5
}
