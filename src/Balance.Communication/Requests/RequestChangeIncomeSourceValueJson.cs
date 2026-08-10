namespace Balance.Communication.Requests;

public class RequestChangeIncomeSourceValueJson
{
    public Guid IncomeSourceId { get; set; }

    public decimal Amount { get; set; }

    public int ExpectedDay { get; set; }

    /// <summary>The day the new value starts. The previous version is closed the day before.</summary>
    public DateOnly ValidityStart { get; set; }

    public string ChangeReason { get; set; } = string.Empty;
}
