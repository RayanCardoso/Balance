using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Expenses;

public interface IExpenseWriteOnlyRepository
{
    Task Add(Expense expense);

    Task AddRange(IEnumerable<Expense> expenses);
}
