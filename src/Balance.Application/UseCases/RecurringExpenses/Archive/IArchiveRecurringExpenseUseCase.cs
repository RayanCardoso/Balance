namespace Balance.Application.UseCases.RecurringExpenses.Archive;

public interface IArchiveRecurringExpenseUseCase
{
    /// <summary>
    /// Sets the archived flag of a recurring expense to <paramref name="archived"/>. Archive and
    /// unarchive are one code path so they share a single ownership check.
    /// </summary>
    Task Execute(Guid recurringExpenseId, bool archived);
}
