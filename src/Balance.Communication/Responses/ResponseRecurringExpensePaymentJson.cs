namespace Balance.Communication.Responses;

public class ResponseRecurringExpensePaymentJson
{
    public Guid Id { get; set; }
    public Guid RecurringExpenseId { get; set; }

    /// <summary>
    /// The version in effect at the reference month, frozen when the payment was recorded. A later
    /// value change never moves it.
    /// </summary>
    public Guid RecurringExpenseVersionId { get; set; }

    public DateOnly ReferenceMonth { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }
    public string? Notes { get; set; }
    public Guid? AccountId { get; set; }
}
