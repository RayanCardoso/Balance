namespace Balance.Communication.Responses;

public class ResponseCreditorSummaryJson
{
    public ResponseCreditorJson Creditor { get; set; } = new();
    public int UnsettledDebtCount { get; set; }
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}
