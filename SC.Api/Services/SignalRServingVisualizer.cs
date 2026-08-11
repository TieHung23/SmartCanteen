using Microsoft.AspNetCore.SignalR;
using SC.Api.Hubs;
using SC.Contract.Services.Visualization;

namespace SC.Api.Services;

/// <summary>
/// Đẩy sự kiện digital-twin tới Unity đang kết nối (group "visualization") qua event "ServingEvent".
/// BEST-EFFORT: nuốt mọi lỗi (Unity sập / hub lỗi KHÔNG được làm hỏng luồng nghiệp vụ).
/// </summary>
public sealed class SignalRServingVisualizer(
    IHubContext<VisualizationHub> hubContext,
    ILogger<SignalRServingVisualizer> logger) : IServingVisualizer
{
    public const string ClientEventName = "ServingEvent";

    public async Task PublishAsync(ServingVisualEvent evt, CancellationToken cancellationToken = default)
    {
        try
        {
            var stamped = evt.TimestampUtc == default
                ? evt with { TimestampUtc = DateTime.UtcNow }
                : evt;

            await hubContext.Clients
                .Group(VisualizationHub.VizGroup)
                .SendAsync(ClientEventName, stamped, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to publish visualization event {Type} for order {OrderId}",
                evt.Type, evt.OrderId);
        }
    }
}
