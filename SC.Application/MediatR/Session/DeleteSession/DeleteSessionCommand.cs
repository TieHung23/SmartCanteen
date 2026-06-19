using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.DeleteSession;

public class DeleteSessionCommand : ICommand<DeleteSessionResponse>
{
    public Guid Id { get; set; }

    public DeleteSessionCommand(Guid id)
    {
        Id = id;
    }
}
