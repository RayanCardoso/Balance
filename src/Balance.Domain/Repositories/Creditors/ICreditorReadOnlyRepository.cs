using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Creditors;

public interface ICreditorReadOnlyRepository
{
    /// <summary>
    /// Every creditor of <paramref name="user"/>, ordered by name. Archived rows are excluded
    /// unless <paramref name="includeArchived"/> is true.
    /// </summary>
    Task<List<Creditor>> GetAll(User user, bool includeArchived);

    /// <summary>The creditor, or null when it is not owned by <paramref name="user"/>.</summary>
    Task<Creditor?> GetById(User user, Guid id);
}
