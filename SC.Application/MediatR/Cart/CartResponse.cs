namespace SC.Application.MediatR.Cart;

public class CartResponse
{
    public Guid? Id { get; set; }
    public CartData Data { get; set; } = new();
    public long Version { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
