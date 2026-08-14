using Balance.Domain.Entities;
using Balance.Domain.Repositories.RecurringExpenses;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.RecurringExpenses;

internal class RecurringExpenseRepository :
    IRecurringExpenseReadOnlyRepository,
    IRecurringExpenseWriteOnlyRepository,
    IRecurringExpenseUpdateOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public RecurringExpenseRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(RecurringExpense recurringExpense) =>
        await _dbContext.RecurringExpenses.AddAsync(recurringExpense);

    public async Task AddVersion(RecurringExpenseVersion version) =>
        await _dbContext.RecurringExpenseVersions.AddAsync(version);

    async Task<RecurringExpense?> IRecurringExpenseReadOnlyRepository.GetById(User user, Guid recurringExpenseId) =>
        await _dbContext
            .RecurringExpenses
            .AsNoTracking()
            .Include(expense => expense.Versions)
            .FirstOrDefaultAsync(expense =>
                expense.Id == recurringExpenseId && expense.Person.UserId == user.Id);

    async Task<RecurringExpense?> IRecurringExpenseUpdateOnlyRepository.GetById(User user, Guid recurringExpenseId) =>
        await _dbContext
            .RecurringExpenses
            .Include(expense => expense.Versions)
            .FirstOrDefaultAsync(expense =>
                expense.Id == recurringExpenseId && expense.Person.UserId == user.Id);

    public async Task<List<RecurringExpense>> GetAll(User user) =>
        await _dbContext
            .RecurringExpenses
            .AsNoTracking()
            .Include(expense => expense.Versions)
            .Where(expense => expense.Person.UserId == user.Id)
            .OrderBy(expense => expense.Name)
            .ToListAsync();

    public async Task<List<RecurringExpense>> GetForMonth(User user, DateOnly competenceMonth)
    {
        var firstDayOfMonth = new DateOnly(competenceMonth.Year, competenceMonth.Month, 1);

        return await _dbContext
            .RecurringExpenses
            .AsNoTracking()
            .Include(expense => expense.Versions)
            .Include(expense => expense.Payments.Where(payment => payment.ReferenceMonth == firstDayOfMonth))
            .Where(expense => expense.Person.UserId == user.Id && expense.Archived == false)
            .OrderBy(expense => expense.Name)
            .ToListAsync();
    }
}
