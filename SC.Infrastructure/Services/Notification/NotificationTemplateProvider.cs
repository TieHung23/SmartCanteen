using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Notification;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Notification;

public sealed partial class NotificationTemplateProvider(
    IOptions<NotificationTemplateOptions> options,
    ILogger<NotificationTemplateProvider> logger) : INotificationTemplateProvider
{
    private readonly NotificationTemplateOptions _options = options.Value;

    public NotificationTemplate? Render(
        string key,
        IReadOnlyDictionary<string, string> tokens)
    {
        if (!_options.Templates.TryGetValue(key, out var definition))
        {
            logger.LogWarning("Notification template {TemplateKey} is not configured", key);
            return null;
        }

        var title = ReplaceTokens(definition.Title, tokens);
        var message = ReplaceTokens(definition.MessageTemplate, tokens);
        var actionUrl = ReplaceTokens(definition.ActionUrlTemplate, tokens);

        if (title is null || message is null
            || (definition.ActionUrlTemplate is not null && actionUrl is null))
        {
            logger.LogWarning(
                "Notification template {TemplateKey} contains unresolved tokens",
                key);
            return null;
        }

        return new NotificationTemplate(
            definition.Type,
            title,
            message,
            definition.ReferenceType,
            actionUrl);
    }

    private static string? ReplaceTokens(
        string? template,
        IReadOnlyDictionary<string, string> tokens)
    {
        if (template is null)
        {
            return null;
        }

        var unresolvedToken = false;
        var result = TemplateTokenRegex().Replace(template, match =>
        {
            var tokenName = match.Groups["name"].Value;
            if (tokens.TryGetValue(tokenName, out var value))
            {
                return value;
            }

            unresolvedToken = true;
            return match.Value;
        });

        return unresolvedToken ? null : result;
    }

    [GeneratedRegex(@"\{(?<name>[A-Za-z0-9_]+)\}")]
    private static partial Regex TemplateTokenRegex();
}
