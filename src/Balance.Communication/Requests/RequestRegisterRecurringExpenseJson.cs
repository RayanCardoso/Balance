using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterRecurringExpenseJson
{
    public string Name { get; set; } = string.Empty;

    public Guid PersonId { get; set; }

    public ExpenseType Type { get; set; }

    public Guid CategoryId { get; set; }

    public Guid AccountId { get; set; }

    /// <summary>The day of the month the bill falls due. Stored as given, never clamped.</summary>
    public int DueDay { get; set; }

    /// <summary>The base value of the bill, carried by the first version.</summary>
    public decimal Amount { get; set; }

    /// <summary>Marks the base value as provisional. Reported on the monthly line.</summary>
    public bool IsEstimate { get; set; }

    /// <summary>The day the first version starts. Defaults to today when omitted.</summary>
    public DateOnly? ValidityStart { get; set; }

    /// <summary>Why the first version carries this amount. Optional on registration.</summary>
    public string? ChangeReason { get; set; }
}
