namespace Balance.Domain.Entities;

public class RecurringExpensePayment : BaseEntity
{
    public Guid RecurringExpenseId { get; set; }
    public RecurringExpense RecurringExpense { get; set; } = null!;

    /// <summary>
    /// The version in effect at the reference month, frozen when the payment was
    /// recorded so a later validity correction cannot rewrite recorded history.
    /// </summary>
    public Guid RecurringExpenseVersionId { get; set; }
    public RecurringExpenseVersion Version { get; set; } = null!;

    /// <summary>Normalised to the first day of the month it refers to.</summary>
    public DateOnly ReferenceMonth { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal AmountPaid { get; set; }

    public string? Notes { get; set; }

    /// <summary>The account that actually paid this month. Null when it was not recorded.</summary>
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
}
