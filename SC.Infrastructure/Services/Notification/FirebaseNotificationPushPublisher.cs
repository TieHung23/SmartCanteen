using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Notification.Entity;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Notification;

public sealed class FirebaseNotificationPushPublisher(
    HttpClient httpClient,
    IGenericRepository<UserDeviceToken, Guid> deviceTokenRepository,
    IUnitOfWork unitOfWork,
    IOptions<FirebaseOptions> options,
    ILogger<FirebaseNotificationPushPublisher> logger)
    : INotificationPushPublisher
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly FirebaseOptions _options = options.Value;

    public async Task PublishAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogDebug(
                "Skipping native push for notification {NotificationId}; Firebase is disabled.",
                notification.Id);
            return;
        }

        var projectId = ResolveProjectId();
        if (string.IsNullOrWhiteSpace(projectId))
        {
            logger.LogWarning(
                "Skipping native push for notification {NotificationId}; Firebase project ID is not configured and could not be read from the service account.",
                notification.Id);
            return;
        }

        var tokens = await deviceTokenRepository
            .GetQueryable(token =>
                token.UserId == notification.RecipientId
                && token.IsActive
                && !token.IsDeleted)
            .OrderByDescending(token => token.LastUsedAtUtc)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            logger.LogDebug(
                "No active device tokens found for notification recipient {RecipientId}",
                notification.RecipientId);
            return;
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var endpoint = BuildSendEndpoint(projectId);
        var successCount = 0;
        var revokedCount = 0;

        foreach (var token in tokens)
        {
            var payload = BuildPayload(notification, token.Token);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                token.MarkUsed(notification.RecipientId);
                successCount++;
                continue;
            }

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (IsRevocable(response.StatusCode, responseBody))
            {
                token.Revoke(notification.RecipientId);
                revokedCount++;
                continue;
            }

            logger.LogWarning(
                "Firebase push failed for notification {NotificationId} and device token {DeviceTokenId}. Status: {StatusCode}. Response: {ResponseBody}",
                notification.Id,
                token.Id,
                (int)response.StatusCode,
                responseBody);
        }

        if (successCount > 0 || revokedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Firebase push completed for notification {NotificationId}. Success: {SuccessCount}, Revoked tokens: {RevokedCount}",
            notification.Id,
            successCount,
            revokedCount);
    }

    private async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        var credential = GetCredential()
            .CreateScoped(GetMessagingScope());

        return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(
            cancellationToken: cancellationToken);
    }

    private GoogleCredential GetCredential()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountJson))
        {
            return GoogleCredential.FromJson(_options.ServiceAccountJson);
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountPath))
        {
            return GoogleCredential.FromFile(_options.ServiceAccountPath);
        }

        throw new InvalidOperationException(
            "Firebase service account is not configured.");
    }

    private string? ResolveProjectId()
    {
        if (!string.IsNullOrWhiteSpace(_options.ProjectId))
        {
            return _options.ProjectId.Trim();
        }

        try
        {
            using var document = !string.IsNullOrWhiteSpace(_options.ServiceAccountJson)
                ? JsonDocument.Parse(_options.ServiceAccountJson)
                : JsonDocument.Parse(File.ReadAllText(_options.ServiceAccountPath!));

            return document.RootElement.TryGetProperty("project_id", out var projectId)
                ? projectId.GetString()
                : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Firebase project ID could not be resolved from the service account.");
            return null;
        }
    }

    private Uri BuildSendEndpoint(string projectId)
    {
        var baseUrl = _options.ApiBaseUrl.Trim().TrimEnd('/');
        return new Uri(
            $"{baseUrl}/v1/projects/{Uri.EscapeDataString(projectId)}/messages:send",
            UriKind.Absolute);
    }

    private string GetMessagingScope()
    {
        return _options.MessagingScope.Trim();
    }

    private static object BuildPayload(
        NotificationDelivery notification,
        string token)
    {
        return new
        {
            message = new
            {
                token,
                notification = new
                {
                    title = notification.Title,
                    body = notification.Message
                },
                data = BuildData(notification),
                android = new
                {
                    priority = "HIGH"
                },
                apns = new
                {
                    payload = new
                    {
                        aps = new
                        {
                            sound = "default"
                        }
                    }
                }
            }
        };
    }

    private static Dictionary<string, string> BuildData(
        NotificationDelivery notification)
    {
        var data = new Dictionary<string, string>
        {
            ["notificationId"] = notification.Id.ToString(),
            ["type"] = notification.Type,
            ["isRead"] = notification.IsRead.ToString(),
            ["createdAtUtc"] = notification.CreatedAtUtc.ToString("O")
        };

        AddIfPresent(data, "referenceType", notification.ReferenceType);
        AddIfPresent(data, "referenceId", notification.ReferenceId?.ToString());
        AddIfPresent(data, "actionUrl", notification.ActionUrl);
        AddIfPresent(data, "dataJson", notification.DataJson);

        return data;
    }

    private static void AddIfPresent(
        IDictionary<string, string> data,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            data[key] = value;
        }
    }

    private static bool IsRevocable(
        HttpStatusCode statusCode,
        string responseBody)
    {
        if (statusCode is HttpStatusCode.NotFound)
        {
            return true;
        }

        if (statusCode is not HttpStatusCode.BadRequest)
        {
            return false;
        }

        return responseBody.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase)
               || responseBody.Contains("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase)
               || responseBody.Contains("SENDER_ID_MISMATCH", StringComparison.OrdinalIgnoreCase);
    }
}
