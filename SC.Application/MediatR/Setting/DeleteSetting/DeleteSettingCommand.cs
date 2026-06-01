using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Setting.DeleteSetting;

public record DeleteSettingCommand(Guid Id) : ICommand<DeleteSettingResponse>;

