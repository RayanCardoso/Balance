using Balance.Domain.Entities;
using Balance.Domain.Repositories.Expenses;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.Expenses;

internal class ExpenseRepository : IExpenseReadOnlyRepository, IExpenseWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public ExpenseRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Expense expense) => await _dbContext.Expenses.AddAsync(expense);

    public async Task AddRange(IEnumerable<Expense> expenses) => await _dbContext.Expenses.AddRangeAsync(expenses);

    public async Task<List<Expense>> GetForMonth(User user, DateOnly competenceMonth)
    {
        var firstDayOfMonth = new DateOnly(competenceMonth.Year, competenceMonth.Month, 1);

        return await _dbContext
            .Expenses
            .AsNoTracking()
            .Include(expense => expense.Category)
            .Include(expense => expense.Account)
            .Include(expense => expense.InstallmentPlan)
            .Where(expense => expense.Person.UserId == user.Id && expense.CompetenceMonth == firstDayOfMonth)
            .OrderBy(expense => expense.Date)
            .ThenBy(expense => expense.Name)
            .ToListAsync();
    }
}
