namespace Balance.Domain.Entities;

// Exists only for Mode = Scheduled. The expectation half of the pair.
public class DebtInstallment : BaseEntity
{
    public Guid DebtId { get; set; }
    public Debt Debt { get; set; } = null!;

    public int Number { get; set; }

    /// <summary>Normalised to the first day of the month it falls in.</summary>
    public DateOnly ReferenceMonth { get; set; }

    /// <summary>The due day inside that month, clamped to the month's length.</summary>
    public DateOnly DueDate { get; set; }

    public decimal ExpectedAmount { get; set; }
}
