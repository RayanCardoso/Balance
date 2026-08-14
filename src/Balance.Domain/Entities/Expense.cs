using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

public class Expense : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ExpenseType Type { get; set; }

    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>
    /// The month this expense belongs to, normalised to the first day. Derived from
    /// the account's closing day unless the request supplied it.
    /// </summary>
    public DateOnly CompetenceMonth { get; set; }

    /// <summary>Null unless this expense is one installment of a plan.</summary>
    public int? InstallmentNumber { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public Guid? InstallmentPlanId { get; set; }
    public InstallmentPlan? InstallmentPlan { get; set; }
}
