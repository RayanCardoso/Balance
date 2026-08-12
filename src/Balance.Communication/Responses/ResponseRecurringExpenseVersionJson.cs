namespace Balance.Communication.Responses;

public class ResponseRecurringExpenseVersionJson
{
    public Guid Id { get; set; }
    public Guid RecurringExpenseId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ValidityStart { get; set; }

    /// <summary>Null while this is the version currently in effect.</summary>
    public DateOnly? ValidityEnd { get; set; }

    public string ChangeReason { get; set; } = string.Empty;
}
