using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.RecurringExpenses;

public interface IRecurringExpenseWriteOnlyRepository
{
    Task Add(RecurringExpense recurringExpense);

    Task AddVersion(RecurringExpenseVersion version);
}
