using System.Text.Json;

namespace SC.Application.MediatR.Cart;

internal static class CartJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(CartData data)
    {
        return JsonSerializer.Serialize(data.Meals, Options);
    }

    public static CartData Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return new CartData
            {
                Meals = JsonSerializer.Deserialize<List<CartMealData>>(json, Options) ?? []
            };
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("mealId", out _))
        {
            var legacyMeal = JsonSerializer.Deserialize<CartMealData>(json, Options);
            return legacyMeal is null
                ? new CartData()
                : new CartData { Meals = [legacyMeal] };
        }

        return JsonSerializer.Deserialize<CartData>(json, Options)
               ?? throw new JsonException("Cart data is empty.");
    }
}
