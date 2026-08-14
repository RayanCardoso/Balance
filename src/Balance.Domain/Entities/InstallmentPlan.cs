namespace Balance.Domain.Entities;

public class InstallmentPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int InstallmentCount { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>The competence month of the last installment.</summary>
    public DateOnly EndDate { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public IList<Expense> Installments { get; set; } = [];
}
