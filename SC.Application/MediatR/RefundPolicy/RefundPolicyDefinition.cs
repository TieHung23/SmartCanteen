using System.Globalization;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.RefundPolicy;

public sealed class RefundPolicyDefinition
{
    public string Scope { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Percent { get; init; }
    public bool RequiresImage { get; init; }

    public static bool TryCreate(
        string scope,
        IEnumerable<SettingAggregate> settings,
        out RefundPolicyDefinition? policy)
    {
        policy = null;
        var values = settings
            .Where(setting => !setting.IsDeleted)
            .GroupBy(setting => setting.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue(RefundPolicyConstants.NameCode, out var nameSetting)
            || string.IsNullOrWhiteSpace(nameSetting.Value)
            || !values.TryGetValue(RefundPolicyConstants.DescriptionCode, out var descriptionSetting)
            || !values.TryGetValue(RefundPolicyConstants.PercentCode, out var percentSetting)
            || !decimal.TryParse(
                percentSetting.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var percent)
            || percent <= 0
            || percent > 100
            || !values.TryGetValue(
                RefundPolicyConstants.RequiresImageCode,
                out var requiresImageSetting)
            || !bool.TryParse(requiresImageSetting.Value, out var requiresImage))
        {
            return false;
        }

        policy = new RefundPolicyDefinition
        {
            Scope = scope,
            Name = nameSetting.Value.Trim(),
            Description = descriptionSetting.Value.Trim(),
            Percent = percent,
            RequiresImage = requiresImage
        };

        return true;
    }
}
