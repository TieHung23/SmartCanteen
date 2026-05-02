namespace SC.Domain.Domain.Payment.ValueObject;

public class BalanceSnapshot : Abstraction.Aggregates.ValueObject
{
    private BalanceSnapshot(decimal deltaAmount, decimal balanceBefore, decimal balanceAfter)
    {
        DeltaAmount = deltaAmount;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
    }

    public decimal DeltaAmount { get; }
    public decimal BalanceBefore { get; }
    public decimal BalanceAfter { get; }

    public static BalanceSnapshot Create(decimal deltaAmount, decimal balanceBefore, decimal balanceAfter)
    {
        return new BalanceSnapshot(deltaAmount, balanceBefore, balanceAfter);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeltaAmount;
        yield return BalanceBefore;
        yield return BalanceAfter;
    }
}