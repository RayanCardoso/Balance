using Balance.Domain.Entities;
using Balance.Domain.Repositories.RecurringExpenses;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.RecurringExpenses;

internal class RecurringExpensePaymentRepository : IRecurringExpensePaymentRepository
{
    private readonly BalanceDbContext _dbContext;

    public RecurringExpensePaymentRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(RecurringExpensePayment payment) =>
        await _dbContext.RecurringExpensePayments.AddAsync(payment);

    public async Task<RecurringExpensePayment?> GetById(User user, Guid paymentId) =>
        await _dbContext
            .RecurringExpensePayments
            .FirstOrDefaultAsync(payment =>
                payment.Id == paymentId && payment.RecurringExpense.Person.UserId == user.Id);

    public async Task<RecurringExpensePayment?> GetByMonth(Guid recurringExpenseId, DateOnly referenceMonth) =>
        await _dbContext
            .RecurringExpensePayments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment =>
                payment.RecurringExpenseId == recurringExpenseId
                && payment.ReferenceMonth == referenceMonth);
}
