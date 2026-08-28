namespace Balance.Communication.Responses;

public class ResponseMonthlyDebtJson
{
    public DateOnly CompetenceMonth { get; set; }

    /// <summary>Every debt line falling in this month, Scheduled and OpenEnded alike.</summary>
    public List<ResponseMonthlyDebtLineJson> Lines { get; set; } = [];

    /// <summary>The sum of every Scheduled line's expected amount. OpenEnded lines contribute nothing.</summary>
    public decimal TotalExpected { get; set; }

    /// <summary>The sum of every line's amount paid.</summary>
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// What the month actually costs: per Scheduled line, the amount paid when it exists and the
    /// expected amount when it does not, plus every OpenEnded line's amount paid.
    /// </summary>
    public decimal TotalCommitted { get; set; }
}
