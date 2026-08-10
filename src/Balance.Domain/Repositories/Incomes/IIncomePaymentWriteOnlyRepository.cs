using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Incomes;

public interface IIncomePaymentWriteOnlyRepository
{
    Task Add(IncomePayment payment);
}
