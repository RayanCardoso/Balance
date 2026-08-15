using Balance.Communication.Enums;

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

    /// <summary>Overrides the recurring expense's own payment type for this month. Null when not recorded.</summary>
    public ExpenseType? Type { get; set; }
}
