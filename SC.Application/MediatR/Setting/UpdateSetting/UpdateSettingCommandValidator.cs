using System.Text.Json;
using FluentValidation;

namespace SC.Application.MediatR.Setting.UpdateSetting;

public class UpdateSettingCommandValidator : AbstractValidator<UpdateSettingCommand>
{
    private static readonly string[] AllowedTypes =
    [
        "string",
        "int",
        "decimal",
        "bool",
        "json",
        "datetime"
    ];

    public UpdateSettingCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Group)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Scope)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9_.-]+$")
            .WithMessage("Scope must contain only letters, numbers, underscore, dot, or hyphen.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .MaximumLength(50)
            .Must(type =>
            {
                var normalized = (type ?? string.Empty).Trim();
                return AllowedTypes.Any(t =>
                    string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.Value)
            .NotEmpty()
            .Must((command, value) => IsValidValueByType(command.Type, value))
            .WithMessage("Value is not valid for the specified Type.");

    }

    private static bool IsValidValueByType(string? type, string? value)
    {
        var normalizedType = (type ?? string.Empty).Trim();
        var normalizedValue = (value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedType))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return false;
        }

        if (normalizedType.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedType.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(normalizedValue, out _);
        }

        if (normalizedType.Equals("decimal", StringComparison.OrdinalIgnoreCase))
        {
            return decimal.TryParse(normalizedValue, out _);
        }

        if (normalizedType.Equals("bool", StringComparison.OrdinalIgnoreCase))
        {
            return bool.TryParse(normalizedValue, out _);
        }

        if (normalizedType.Equals("datetime", StringComparison.OrdinalIgnoreCase))
        {
            return DateTimeOffset.TryParse(normalizedValue, out _);
        }

        if (normalizedType.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                JsonDocument.Parse(normalizedValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
