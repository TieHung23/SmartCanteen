using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Setting.CreateSetting;

public class CreateSettingCommand : ICommand<CreateSettingResponse>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
