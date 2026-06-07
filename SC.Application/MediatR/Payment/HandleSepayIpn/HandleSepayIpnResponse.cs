namespace SC.Application.MediatR.Payment.HandleSepayIpn;

public class HandleSepayIpnResponse
{
    public Guid PaymentId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ConvertedPoints { get; set; }
}
