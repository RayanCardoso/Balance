using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.RecurringExpenses;

public interface IRecurringExpenseReadOnlyRepository
{
    /// <summary>
    /// The recurring expense with its versions, or null when it is not owned by <paramref name="user"/>.
    /// </summary>
    Task<RecurringExpense?> GetById(User user, Guid recurringExpenseId);

    /// <summary>
    /// Every recurring expense of the user, archived or not, carrying all its versions.
    ///
    /// This is the only surface an archived bill remains reachable through: <see cref="GetForMonth"/>
    /// deliberately excludes archived rows from a month, which is correct for that view but would
    /// leave an archived bill's id undiscoverable anywhere else - unreachable to unarchive.
    /// </summary>
    Task<List<RecurringExpense>> GetAll(User user);

    /// <summary>
    /// Every non-archived recurring expense of the user, carrying all its versions and only the
    /// payments whose reference month is <paramref name="competenceMonth"/>.
    /// </summary>
    Task<List<RecurringExpense>> GetForMonth(User user, DateOnly competenceMonth);
}
