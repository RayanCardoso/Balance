namespace Balance.Communication.Requests;

/// <summary>
/// A correction to a recorded payment. It carries neither the reference month nor the frozen version
/// identifier: a correction changes what was paid, never which month it belongs to nor which version
/// it was measured against.
/// </summary>
public class RequestUpdateRecurringExpensePaymentJson
{
    public DateOnly PaymentDate { get; set; }

    public decimal AmountPaid { get; set; }

    public string? Notes { get; set; }

    /// <summary>The account that actually paid this month. Optional.</summary>
    public Guid? AccountId { get; set; }
}
