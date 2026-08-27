using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

// Owned by the User, not a Person: the household shares one catalogue of who it owes (AD-005).
public class Creditor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CreditorType Type { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
    public bool Archived { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
