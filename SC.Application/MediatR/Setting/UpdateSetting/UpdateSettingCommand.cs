using System.Text.Json.Serialization;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Setting.UpdateSetting;

public class UpdateSettingCommand : ICommand<UpdateSettingResponse>
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
