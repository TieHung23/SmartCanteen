using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommand : ICommand<UnregisterDeviceTokenResponse>
{
    public string? Token { get; set; }
    public string? DeviceId { get; set; }
}
