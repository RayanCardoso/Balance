using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Debts;

public interface IDebtInstallmentWriteOnlyRepository
{
    Task AddRange(IEnumerable<DebtInstallment> installments);
}
