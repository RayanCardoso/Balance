using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Incomes;

public interface IIncomeSourceWriteOnlyRepository
{
    Task Add(IncomeSource incomeSource);

    Task AddVersion(IncomeSourceVersion version);
}
