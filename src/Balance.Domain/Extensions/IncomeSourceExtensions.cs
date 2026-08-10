using Balance.Domain.Entities;

namespace Balance.Domain.Extensions;

public static class IncomeSourceExtensions
{
    public static DateOnly FirstDayOfMonth(this DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>
    /// The version in effect at a reference month: the one with the greatest
    /// <see cref="IncomeSourceVersion.ValidityStart"/> that is not after the last day of the
    /// month and whose <see cref="IncomeSourceVersion.ValidityEnd"/> is null or not before the
    /// first day. Returns null when the month predates every version, or when the source is
    /// Variable and therefore has none.
    /// </summary>
    public static IncomeSourceVersion? VersionInEffect(this IncomeSource source, DateOnly referenceMonth)
    {
        var firstDayOfMonth = referenceMonth.FirstDayOfMonth();
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        return source.Versions
            .Where(version =>
                version.ValidityStart <= lastDayOfMonth
                && (version.ValidityEnd is null || version.ValidityEnd >= firstDayOfMonth))
            .OrderByDescending(version => version.ValidityStart)
            .FirstOrDefault();
    }
}
