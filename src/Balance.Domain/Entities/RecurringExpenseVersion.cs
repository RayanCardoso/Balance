namespace Balance.Domain.Entities;

public class RecurringExpenseVersion : BaseEntity
{
    public Guid RecurringExpenseId { get; set; }
    public RecurringExpense RecurringExpense { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateOnly ValidityStart { get; set; }

    /// <summary>Null while this is the version currently in effect.</summary>
    public DateOnly? ValidityEnd { get; set; }

    public string ChangeReason { get; set; } = string.Empty;
}
