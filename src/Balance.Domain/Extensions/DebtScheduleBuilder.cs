namespace Balance.Domain.Extensions;

public static class DebtScheduleBuilder
{
    /// <summary>
    /// The first competence month a scheduled debt is due against. A debt taken out on or before
    /// its own due day still has time to make that month's payment, so the schedule starts there;
    /// taken out after the due day has already passed, so the schedule starts the month after.
    /// This is a due-day question, not a closing-day one - it has nothing to do with
    /// <see cref="CompetenceMonthResolver"/>, which resolves a credit-card invoice month instead.
    /// </summary>
    public static DateOnly FirstCompetenceMonth(DateOnly startDate, int dueDay)
    {
        var month = startDate.Day <= dueDay ? startDate : startDate.AddMonths(1);

        return month.FirstDayOfMonth();
    }

    /// <summary>
    /// The due date within a given competence month. A due day chosen for a long month (e.g. 31)
    /// has no literal match in a shorter one, so it clamps to that month's last day instead of
    /// overflowing into the next.
    /// </summary>
    public static DateOnly DueDateIn(DateOnly competenceMonth, int dueDay)
    {
        var day = Math.Min(dueDay, DateTime.DaysInMonth(competenceMonth.Year, competenceMonth.Month));

        return new DateOnly(competenceMonth.Year, competenceMonth.Month, day);
    }
}
