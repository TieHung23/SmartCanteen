using SC.Domain.Abstraction.Aggregates;

namespace SC.Domain.Domain.User;

public class Money: ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Money cannot be negative.");
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "VND") 
        => new Money(amount, currency);
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Cannot add money with different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}