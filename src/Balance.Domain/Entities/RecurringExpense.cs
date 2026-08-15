using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

public class RecurringExpense : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ExpenseType Type { get; set; }

    public int DueDay { get; set; }

    /// <summary>Marks the base value as provisional. Reported on the monthly line; never a validation rule.</summary>
    public bool IsEstimate { get; set; }

    public bool Archived { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid? AccountId { get; set; }
    public Account? Account { get; set; } = null!;

    public IList<RecurringExpenseVersion> Versions { get; set; } = [];
    public IList<RecurringExpensePayment> Payments { get; set; } = [];
}
