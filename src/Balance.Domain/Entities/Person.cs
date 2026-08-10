namespace Balance.Domain.Entities;

public class Person : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsAccountOwner { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
