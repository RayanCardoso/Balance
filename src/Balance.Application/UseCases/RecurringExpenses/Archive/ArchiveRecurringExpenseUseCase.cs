using Balance.Domain.Repositories;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.RecurringExpenses.Archive;

public class ArchiveRecurringExpenseUseCase : IArchiveRecurringExpenseUseCase
{
    private readonly IRecurringExpenseUpdateOnlyRepository _updateOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public ArchiveRecurringExpenseUseCase(
        IRecurringExpenseUpdateOnlyRepository updateOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _updateOnlyRepository = updateOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task Execute(Guid recurringExpenseId, bool archived)
    {
        var loggedUser = await _loggedUser.Get();

        var recurringExpense = await _updateOnlyRepository.GetById(loggedUser, recurringExpenseId)
            ?? throw new NotFoundException(ResourceErrorMessages.RECURRING_EXPENSE_NOT_FOUND);

        recurringExpense.Archived = archived;

        await _unitOfWork.Commit();
    }
}
