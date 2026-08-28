using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Debts;

public interface IDebtWriteOnlyRepository
{
    Task Add(Debt debt);
}
