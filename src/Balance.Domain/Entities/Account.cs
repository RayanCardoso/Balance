namespace Balance.Domain.Entities;

/// <summary>
/// A payment account. Owned by a Person, since each person has their own cards.
/// </summary>
public class Account : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    /// <summary>Null on an account that is not a credit card.</summary>
    public int? ClosingDay { get; set; }

    /// <summary>Null on an account that is not a credit card.</summary>
    public int? DueDay { get; set; }

    /// <summary>Null on an account that is not a credit card. Stored for display only.</summary>
    public decimal? Limit { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
}
