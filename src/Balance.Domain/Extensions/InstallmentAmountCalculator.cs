namespace Balance.Domain.Extensions;

public static class InstallmentAmountCalculator
{
    /// <summary>
    /// Splits a total into <paramref name="count"/> parts so they sum to the total exactly, not
    /// by rounding luck. Parts 1..N-1 carry the rounded share; part N carries whatever is left
    /// over, because rounding every part independently can drift a cent away from the total. An
    /// installment purchase and a debt repaid over N months follow the same rule, so both callers
    /// share this one implementation instead of drifting apart under maintenance.
    /// </summary>
    public static IReadOnlyList<decimal> Split(decimal total, int count)
    {
        var each = Math.Round(total / count, 2, MidpointRounding.AwayFromZero);
        var residual = total - each * (count - 1);

        var parts = new List<decimal>(count);

        for (var number = 1; number <= count; number++)
        {
            parts.Add(number == count ? residual : each);
        }

        return parts;
    }
}
