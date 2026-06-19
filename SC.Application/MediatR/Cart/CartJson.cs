using System.Text.Json;

namespace SC.Application.MediatR.Cart;

internal static class CartJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(CartData data)
    {
        return JsonSerializer.Serialize(data.Sessions, Options);
    }

    public static CartData Deserialize(string json)
    {
        json = json
            .Replace("\"mealId\"", "\"sessionId\"")
            .Replace("\"meals\"", "\"sessions\"");

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return new CartData
            {
                Sessions = JsonSerializer.Deserialize<List<CartSessionData>>(json, Options) ?? []
            };
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("sessionId", out _))
        {
            var legacySession = JsonSerializer.Deserialize<CartSessionData>(json, Options);
            return legacySession is null
                ? new CartData()
                : new CartData { Sessions = [legacySession] };
        }

        return JsonSerializer.Deserialize<CartData>(json, Options)
               ?? throw new JsonException("Cart data is empty.");
    }
}
