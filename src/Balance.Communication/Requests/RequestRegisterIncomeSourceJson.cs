using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterIncomeSourceJson
{
    public string Name { get; set; } = string.Empty;

    public IncomeType Type { get; set; }

    public Guid PersonId { get; set; }

    /// <summary>Required for Recurring, must be absent for Variable.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Required for Recurring, must be absent for Variable.</summary>
    public int? ExpectedDay { get; set; }

    /// <summary>Defaults to today when omitted. Ignored for Variable.</summary>
    public DateOnly? ValidityStart { get; set; }

    public string? ChangeReason { get; set; }
}
