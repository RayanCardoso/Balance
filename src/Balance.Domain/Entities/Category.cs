using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

/// <summary>
/// A spending category. Owned by the User directly, so every Person of the
/// account files expenses against the same catalogue.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ExpensePriority Priority { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
