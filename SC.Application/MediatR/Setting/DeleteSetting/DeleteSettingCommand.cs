using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Setting.DeleteSetting;

public class DeleteSettingCommand : ICommand
{
    public Guid Id { get; set; }

    public DeleteSettingCommand()
    {
    }

    public DeleteSettingCommand(Guid id)
    {
        Id = id;
    }
}

