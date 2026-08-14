namespace Balance.Communication.Responses;

/// <summary>
/// One month of income and expenses side by side. Both halves are the untouched responses of the two
/// monthly use cases, so a client reading this gets exactly what the individual endpoints return.
/// </summary>
public class ResponseMonthlyDashboardJson
{
    public DateOnly CompetenceMonth { get; set; }

    public ResponseMonthlyIncomeJson Income { get; set; } = new();

    public ResponseMonthlyExpenseJson Expenses { get; set; } = new();

    /// <summary>
    /// What is left of the month: the income actually received minus what the month costs, counting a
    /// recurring bill at its paid amount once it has arrived and at its estimate while it has not.
    /// </summary>
    public decimal Balance { get; set; }
}
