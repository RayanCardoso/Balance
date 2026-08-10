using Balance.Domain.Entities;
using Balance.Domain.Repositories.Incomes;

namespace Balance.Infrastructure.DataAccess.Repositories.Incomes;

internal class IncomePaymentRepository : IIncomePaymentWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public IncomePaymentRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(IncomePayment payment) => await _dbContext.IncomePayments.AddAsync(payment);
}
