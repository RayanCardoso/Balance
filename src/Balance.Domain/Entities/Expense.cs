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

    /// <summary>
    /// Null when the expense was not paid from a registered account — a Pix or a debit
    /// purchase the user did not attach to one. A credit expense always carries an account:
    /// it is that account's closing day that decides which month the purchase belongs to.
    /// </summary>
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }

    public Guid? InstallmentPlanId { get; set; }
    public InstallmentPlan? InstallmentPlan { get; set; }
}
