using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.RecurringExpenses;

public interface IRecurringExpenseUpdateOnlyRepository
{
    /// <summary>
    /// Tracked read, including versions, so a value change can close the one in effect and the
    /// archive operation can flip the flag.
    /// </summary>
    Task<RecurringExpense?> GetById(User user, Guid recurringExpenseId);
}
