using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Creditors;

public interface ICreditorUpdateOnlyRepository
{
    /// <summary>
    /// Tracked read, so an edit or an archive toggle can be persisted in place. Null when the
    /// creditor is not owned by <paramref name="user"/>.
    /// </summary>
    Task<Creditor?> GetById(User user, Guid id);
}
