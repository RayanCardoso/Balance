namespace Balance.Domain.Entities;

public class IncomePayment : BaseEntity
{
    public Guid IncomeSourceId { get; set; }
    public IncomeSource IncomeSource { get; set; } = null!;

    /// <summary>
    /// The version in effect when the payment was recorded. Null for a Variable
    /// income source, which has no versions.
    /// </summary>
    public Guid? IncomeSourceVersionId { get; set; }
    public IncomeSourceVersion? IncomeSourceVersion { get; set; }

    public DateOnly PaymentDate { get; set; }

    /// <summary>Normalised to the first day of the month it refers to.</summary>
    public DateOnly ReferenceMonth { get; set; }

    public decimal AmountReceived { get; set; }

    public string? Notes { get; set; }
}
