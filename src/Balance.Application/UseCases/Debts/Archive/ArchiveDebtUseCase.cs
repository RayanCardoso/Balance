using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Debts.Archive;

public class ArchiveDebtUseCase : IArchiveDebtUseCase
{
    private readonly IDebtUpdateOnlyRepository _updateOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public ArchiveDebtUseCase(
        IDebtUpdateOnlyRepository updateOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _updateOnlyRepository = updateOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task Execute(Guid id, bool archived)
    {
        var loggedUser = await _loggedUser.Get();

        // Resolved through the update-only repository, whose read is tracked - the read-only
        // repository is AsNoTracking, so setting Archived through it and calling Commit() would
        // persist nothing while every mocked test still passed.
        var debt = await _updateOnlyRepository.GetById(loggedUser, id)
            ?? throw new NotFoundException(ResourceErrorMessages.DEBT_NOT_FOUND);

        debt.Archived = archived;

        await _unitOfWork.Commit();
    }
}
