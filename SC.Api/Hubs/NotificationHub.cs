using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SC.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub;
