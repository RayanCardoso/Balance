namespace Balance.Communication.Responses;

public class ResponseIncomeSourceVersionJson
{
    public Guid Id { get; set; }
    public Guid IncomeSourceId { get; set; }
    public decimal Amount { get; set; }
    public int ExpectedDay { get; set; }
    public DateOnly ValidityStart { get; set; }
    public DateOnly? ValidityEnd { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
}
