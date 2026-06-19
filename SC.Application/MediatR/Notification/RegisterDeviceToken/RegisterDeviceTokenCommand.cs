using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommand : ICommand<DeviceTokenResponse>
{
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? AppVersion { get; set; }
}
