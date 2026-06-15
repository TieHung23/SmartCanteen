using System.Text.Json;

namespace SC.Application.MediatR.Cart;

internal static class CartJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(CartData data)
    {
        return JsonSerializer.Serialize(data, Options);
    }

    public static CartData Deserialize(string json)
    {
        return JsonSerializer.Deserialize<CartData>(json, Options)
               ?? throw new JsonException("Cart data is empty.");
    }
}
