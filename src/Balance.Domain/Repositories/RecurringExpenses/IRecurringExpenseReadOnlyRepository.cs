using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.RecurringExpenses;

public interface IRecurringExpenseReadOnlyRepository
{
    /// <summary>
    /// The recurring expense with its versions, or null when it is not owned by <paramref name="user"/>.
    /// </summary>
    Task<RecurringExpense?> GetById(User user, Guid recurringExpenseId);

    /// <summary>
    /// Every non-archived recurring expense of the user, carrying all its versions and only the
    /// payments whose reference month is <paramref name="competenceMonth"/>.
    /// </summary>
    Task<List<RecurringExpense>> GetForMonth(User user, DateOnly competenceMonth);
}
