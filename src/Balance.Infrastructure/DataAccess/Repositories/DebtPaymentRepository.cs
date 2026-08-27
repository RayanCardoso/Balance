using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories;

internal class DebtPaymentRepository : IDebtPaymentRepository
{
    private readonly BalanceDbContext _dbContext;

    public DebtPaymentRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(DebtPayment payment) => await _dbContext.DebtPayments.AddAsync(payment);

    public async Task<DebtPayment?> GetById(User user, Guid id) =>
        await _dbContext
            .DebtPayments
            .FirstOrDefaultAsync(payment => payment.Id == id && payment.Debt.Person.UserId == user.Id);

    public async Task<DebtPayment?> GetByInstallment(User user, Guid installmentId) =>
        await _dbContext
            .DebtPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment =>
                payment.DebtInstallmentId == installmentId && payment.Debt.Person.UserId == user.Id);
}
