using Balance.Domain.Enums;

namespace Balance.Domain.Extensions;

public static class CompetenceMonthResolver
{
    /// <summary>
    /// The month an expense belongs to, normalised to its first day. A credit purchase made
    /// after the account's closing day lands on the following month's invoice; a purchase made
    /// on the closing day itself does not. Every other combination stays in the month of the
    /// date, including credit on an account that carries no closing day.
    /// </summary>
    public static DateOnly Resolve(ExpenseType type, int? closingDay, DateOnly date)
    {
        var rollsToNextMonth = type == ExpenseType.Credit
            && closingDay is not null
            && date.Day > closingDay;

        var month = rollsToNextMonth ? date.AddMonths(1) : date;

        return month.FirstDayOfMonth();
    }
}
