using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Incomes;

public interface IIncomeSourceReadOnlyRepository
{
    /// <summary>The source with its versions, or null when it is not owned by <paramref name="user"/>.</summary>
    Task<IncomeSource?> GetById(User user, Guid incomeSourceId);

    /// <summary>
    /// Every non-archived source of the user, carrying all its versions and only the
    /// payments whose reference month is <paramref name="referenceMonth"/>.
    /// </summary>
    Task<List<IncomeSource>> GetForMonth(User user, DateOnly referenceMonth);
}
