using SC.Domain.Abstraction.Aggregates;

namespace SC.Domain.Domain.Payment;

public class BalanceSnapshot : ValueObject
{
    public decimal DeltaAmount { get; }
    public decimal BalanceBefore { get; }
    public decimal BalanceAfter { get; }

    private BalanceSnapshot(decimal deltaAmount, decimal balanceBefore, decimal balanceAfter)
    {
        DeltaAmount = deltaAmount;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
    }

    public static BalanceSnapshot Create(decimal deltaAmount, decimal balanceBefore, decimal balanceAfter)
        => new(deltaAmount, balanceBefore, balanceAfter);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeltaAmount;
        yield return BalanceBefore;
        yield return BalanceAfter;
    }
}

