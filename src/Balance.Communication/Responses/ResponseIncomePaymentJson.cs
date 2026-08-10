namespace Balance.Communication.Responses;

public class ResponseIncomePaymentJson
{
    public Guid Id { get; set; }
    public Guid IncomeSourceId { get; set; }

    /// <summary>The version frozen at the moment of payment. Null for a Variable source.</summary>
    public Guid? IncomeSourceVersionId { get; set; }

    public DateOnly PaymentDate { get; set; }
    public DateOnly ReferenceMonth { get; set; }
    public decimal AmountReceived { get; set; }
    public string? Notes { get; set; }
}
