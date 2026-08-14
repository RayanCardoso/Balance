namespace Balance.Communication.Requests;

public class RequestChangeRecurringExpenseValueJson
{
    public Guid RecurringExpenseId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>The day the new value starts. The version in effect is closed the day before.</summary>
    public DateOnly ValidityStart { get; set; }

    public string ChangeReason { get; set; } = string.Empty;
}
