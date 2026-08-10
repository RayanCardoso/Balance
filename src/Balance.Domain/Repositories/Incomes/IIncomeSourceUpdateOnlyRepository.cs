using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Incomes;

public interface IIncomeSourceUpdateOnlyRepository
{
    /// <summary>Tracked read, including versions, so a value change can close the current one.</summary>
    Task<IncomeSource?> GetById(User user, Guid incomeSourceId);
}
