namespace Balance.Application.UseCases.Debts.Archive;

public interface IArchiveDebtUseCase
{
    /// <summary>
    /// Sets the archived flag of a debt to <paramref name="archived"/>. Archive and unarchive
    /// are one code path so they share a single ownership check.
    /// </summary>
    Task Execute(Guid id, bool archived);
}
