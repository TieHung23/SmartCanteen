using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Payment.HandleSepayIpn;

public class HandleSepayIpnCommand : ICommand<HandleSepayIpnResponse>
{
    public IReadOnlyDictionary<string, string> Data { get; set; } =
        new Dictionary<string, string>();
}
