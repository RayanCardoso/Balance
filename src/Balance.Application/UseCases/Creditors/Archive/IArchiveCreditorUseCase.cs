namespace Balance.Application.UseCases.Creditors.Archive;

public interface IArchiveCreditorUseCase
{
    /// <summary>
    /// Sets the archived flag of a creditor to <paramref name="archived"/>. Archive and unarchive
    /// are one code path so they share a single ownership check.
    /// </summary>
    Task Execute(Guid id, bool archived);
}
