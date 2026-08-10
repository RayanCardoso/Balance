namespace Balance.Communication.Requests;

public class RequestRegisterIncomePaymentJson
{
    public Guid IncomeSourceId { get; set; }

    public DateOnly PaymentDate { get; set; }

    /// <summary>Any day inside the month it refers to; normalised to the first day.</summary>
    public DateOnly ReferenceMonth { get; set; }

    public decimal AmountReceived { get; set; }

    public string? Notes { get; set; }
}
