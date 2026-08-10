namespace Balance.Domain.Entities;

public class IncomeSourceVersion : BaseEntity
{
    public Guid IncomeSourceId { get; set; }
    public IncomeSource IncomeSource { get; set; } = null!;

    public decimal Amount { get; set; }

    public int ExpectedDay { get; set; }

    public DateOnly ValidityStart { get; set; }

    /// <summary>Null while this is the version currently in effect.</summary>
    public DateOnly? ValidityEnd { get; set; }

    public string ChangeReason { get; set; } = string.Empty;
}
