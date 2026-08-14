using Balance.Domain.Entities;

namespace Balance.Domain.Extensions;

public static class RecurringExpenseExtensions
{
    /// <summary>
    /// The version in effect at a competence month: the one with the greatest
    /// <see cref="RecurringExpenseVersion.ValidityStart"/> that is not after the last day of the
    /// month and whose <see cref="RecurringExpenseVersion.ValidityEnd"/> is null or not before the
    /// first day. Returns null when the month predates every version, or falls after the last one
    /// was closed.
    /// </summary>
    /// <remarks>
    /// Deliberately duplicated from <see cref="IncomeSourceExtensions.VersionInEffect"/> rather than
    /// extracted: income code is read-only for this feature. See the design's approach exploration.
    /// </remarks>
    public static RecurringExpenseVersion? VersionInEffect(this RecurringExpense expense, DateOnly competenceMonth)
    {
        var firstDayOfMonth = competenceMonth.FirstDayOfMonth();
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        return expense.Versions
            .Where(version =>
                version.ValidityStart <= lastDayOfMonth
                && (version.ValidityEnd is null || version.ValidityEnd >= firstDayOfMonth))
            .OrderByDescending(version => version.ValidityStart)
            .FirstOrDefault();
    }
}
