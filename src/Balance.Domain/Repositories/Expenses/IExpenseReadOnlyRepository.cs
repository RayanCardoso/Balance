using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Expenses;

public interface IExpenseReadOnlyRepository
{
    Task<List<Expense>> GetForMonth(User user, DateOnly competenceMonth);
}
