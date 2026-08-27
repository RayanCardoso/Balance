using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Debts;

public interface IDebtReadOnlyRepository
{
    /// <summary>
    /// Every debt of <paramref name="user"/>, with its creditor, category and payments, optionally
    /// narrowed to one creditor or one person. Archived debts are excluded unless
    /// <paramref name="includeInactive"/> is true. Settled debts are never filtered here - settled
    /// is derived from <c>Payments</c> in the application layer, not expressible in SQL. Ordered by
    /// <see cref="Debt.StartDate"/> descending.
    /// </summary>
    Task<List<Debt>> GetAll(User user, Guid? creditorId, Guid? personId, bool includeInactive);

    /// <summary>
    /// The debt with its creditor, category, installments and payments, or null when it is not
    /// owned by <paramref name="user"/>.
    /// </summary>
    Task<Debt?> GetById(User user, Guid id);

    /// <summary>
    /// Every non-archived debt of <paramref name="user"/> with a line in
    /// <paramref name="competenceMonth"/> - a scheduled installment due that month, or an
    /// open-ended payment recorded in it. <c>Installments</c> and <c>Payments</c> are filtered to
    /// that month inside the include, alongside <c>Creditor</c> and <c>Category</c>.
    /// </summary>
    Task<List<Debt>> GetForMonth(User user, DateOnly competenceMonth);

    /// <summary>Every debt of <paramref name="user"/> owed to <paramref name="creditorId"/>, with its payments.</summary>
    Task<List<Debt>> GetByCreditor(User user, Guid creditorId);
}
