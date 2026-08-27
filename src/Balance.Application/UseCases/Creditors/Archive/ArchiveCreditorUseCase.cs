using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Creditors;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Creditors.Archive;

public class ArchiveCreditorUseCase : IArchiveCreditorUseCase
{
    private readonly ICreditorUpdateOnlyRepository _updateOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public ArchiveCreditorUseCase(
        ICreditorUpdateOnlyRepository updateOnlyRepository,
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

        var creditor = await _updateOnlyRepository.GetById(loggedUser, id)
            ?? throw new NotFoundException(ResourceErrorMessages.CREDITOR_NOT_FOUND);

        creditor.Archived = archived;

        await _unitOfWork.Commit();
    }
}
