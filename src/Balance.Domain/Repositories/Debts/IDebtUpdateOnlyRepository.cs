using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Debts;

public interface IDebtUpdateOnlyRepository
{
    /// <summary>
    /// Tracked read, including installments and payments, so an archive toggle or a correction can
    /// be persisted in place. Null when the debt is not owned by <paramref name="user"/>.
    /// </summary>
    Task<Debt?> GetById(User user, Guid id);
}
