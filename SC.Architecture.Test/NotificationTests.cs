using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using SC.Application.MediatR.Notification.Admin.CreateNotification;
using SC.Contract.Services.Notification;
using SC.Domain.Domain.Notification.AggregateRoot;
using SC.Infrastructure.DependencyInjection.Options;
using SC.Infrastructure.Services.Notification;

namespace SC.Architecture.Test;

public sealed class NotificationTests
{
    [Fact]
    public void Create_Should_Reject_Empty_Recipient()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            Guid.Empty,
            "Order.StatusChanged",
            "Title",
            "Message"));
    }

    [Fact]
    public void Create_Should_Require_Reference_Type_When_Reference_Id_Is_Set()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            Guid.NewGuid(),
            "Order.StatusChanged",
            "Title",
            "Message",
            referenceId: Guid.NewGuid()));
    }

    [Fact]
    public void MarkAsRead_Should_Be_Idempotent()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            "Order.StatusChanged",
            "Title",
            "Message");
        var userId = Guid.NewGuid();

        notification.MarkAsRead(userId);
        var firstReadAt = notification.ReadAtUtc;
        notification.MarkAsRead(Guid.NewGuid());

        Assert.True(notification.IsRead);
        Assert.Equal(firstReadAt, notification.ReadAtUtc);
        Assert.Equal(userId, notification.UpdatedBy);
    }

    [Theory]
    [InlineData("https://malicious.example")]
    [InlineData("//malicious.example")]
    [InlineData("javascript:alert(1)")]
    public void CreateCommandValidator_Should_Reject_Unsafe_ActionUrl(string actionUrl)
    {
        var validator = new CreateNotificationCommandValidator();
        var result = validator.Validate(new CreateNotificationCommand
        {
            RecipientId = Guid.NewGuid(),
            Type = "Order.StatusChanged",
            Title = "Title",
            Message = "Message",
            ActionUrl = actionUrl
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TemplateProvider_Should_Render_All_Configured_Tokens()
    {
        var options = Options.Create(new NotificationTemplateOptions
        {
            Templates = new Dictionary<string, NotificationTemplateDefinition>
            {
                [NotificationTemplateKeys.OrderStatusChanged] = new()
                {
                    Type = "Order.StatusChanged",
                    Title = "Order updated",
                    MessageTemplate = "Status: {status}",
                    ReferenceType = "Order",
                    ActionUrlTemplate = "/orders/{referenceId}"
                }
            }
        });
        var provider = new NotificationTemplateProvider(
            options,
            NullLogger<NotificationTemplateProvider>.Instance);

        var template = provider.Render(
            NotificationTemplateKeys.OrderStatusChanged,
            new Dictionary<string, string>
            {
                ["status"] = "Completed",
                ["referenceId"] = "order-id"
            });

        Assert.NotNull(template);
        Assert.Equal("Status: Completed", template.MessageTemplate);
        Assert.Equal("/orders/order-id", template.ActionUrlTemplate);
    }

    [Fact]
    public void TemplateProvider_Should_Return_Null_For_Unresolved_Token()
    {
        var options = Options.Create(new NotificationTemplateOptions
        {
            Templates = new Dictionary<string, NotificationTemplateDefinition>
            {
                [NotificationTemplateKeys.OrderStatusChanged] = new()
                {
                    Type = "Order.StatusChanged",
                    Title = "Order updated",
                    MessageTemplate = "Status: {status}"
                }
            }
        });
        var provider = new NotificationTemplateProvider(
            options,
            NullLogger<NotificationTemplateProvider>.Instance);

        var template = provider.Render(
            NotificationTemplateKeys.OrderStatusChanged,
            new Dictionary<string, string>());

        Assert.Null(template);
    }

    [Fact]
    public void ExampleConfiguration_Should_Contain_All_Business_Template_Keys()
    {
        var solutionDirectory = FindSolutionDirectory();
        var configurationPath = Path.Combine(
            solutionDirectory,
            "SC.Api",
            "appsettings.Notification.example.json");
        using var document = JsonDocument.Parse(File.ReadAllText(configurationPath));
        var configuredTemplates = document.RootElement
            .GetProperty("NotificationTemplates")
            .GetProperty("Templates");

        var templateKeys = typeof(NotificationTemplateKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => Assert.IsType<string>(field.GetRawConstantValue()))
            .ToList();

        Assert.Equal(templateKeys.Count, templateKeys.Distinct().Count());
        foreach (var templateKey in templateKeys)
        {
            Assert.True(
                configuredTemplates.TryGetProperty(templateKey, out _),
                $"Notification template '{templateKey}' is missing from the example configuration.");
        }
    }

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SmartCanteen.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("SmartCanteen solution directory was not found.");
    }
}
