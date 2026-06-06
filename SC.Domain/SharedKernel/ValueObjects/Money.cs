namespace SC.Domain.SharedKernel.ValueObjects;

public class Money : Abstraction.Aggregates.ValueObject
{
    private Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Money cannot be negative.");
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Create(decimal amount, string currency = "Point")
    {
        return new Money(amount, currency);
    }

    public Money Add(Money other)
    {
        return Currency != other.Currency
            ? throw new InvalidOperationException("Cannot add money with different currencies.")
            : new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
