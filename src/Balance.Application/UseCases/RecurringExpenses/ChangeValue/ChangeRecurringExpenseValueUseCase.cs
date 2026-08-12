using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.RecurringExpenses.ChangeValue;

// SPEC_DEVIATION: the design's error table names no not-found key for a recurring expense.
// Reason: the existing keys all name a different entity, so a foreign recurring expense would have
// answered "Income source not found." RECURRING_EXPENSE_NOT_FOUND follows the same correction T18
// applied to CATEGORY_NOT_FOUND and ACCOUNT_NOT_FOUND. The 404 status AD-004 pins is unchanged.
public class ChangeRecurringExpenseValueUseCase : IChangeRecurringExpenseValueUseCase
{
    private readonly IRecurringExpenseUpdateOnlyRepository _updateOnlyRepository;
    private readonly IRecurringExpenseWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public ChangeRecurringExpenseValueUseCase(
        IRecurringExpenseUpdateOnlyRepository updateOnlyRepository,
        IRecurringExpenseWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _updateOnlyRepository = updateOnlyRepository;
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseRecurringExpenseJson> Execute(RequestChangeRecurringExpenseValueJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var recurringExpense = await _updateOnlyRepository.GetById(loggedUser, request.RecurringExpenseId)
            ?? throw new NotFoundException(ResourceErrorMessages.RECURRING_EXPENSE_NOT_FOUND);

        var currentVersion = recurringExpense.VersionInEffect(request.ValidityStart);

        // A null version in effect means the new start predates every version, which is the same
        // failure the spec names: the new start is not later than the one it would replace.
        if (currentVersion is null || request.ValidityStart <= currentVersion.ValidityStart)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.VALIDITY_START_MUST_BE_LATER]);
        }

        currentVersion.ValidityEnd = request.ValidityStart.AddDays(-1);

        var newVersion = new RecurringExpenseVersion
        {
            RecurringExpenseId = recurringExpense.Id,
            Amount = request.Amount,
            ValidityStart = request.ValidityStart,
            ValidityEnd = null,
            ChangeReason = request.ChangeReason
        };

        await _writeOnlyRepository.AddVersion(newVersion);

        await _unitOfWork.Commit();

        return new ResponseRecurringExpenseJson
        {
            Id = recurringExpense.Id,
            Name = recurringExpense.Name,
            PersonId = recurringExpense.PersonId,
            CategoryId = recurringExpense.CategoryId,
            AccountId = recurringExpense.AccountId,
            DueDay = recurringExpense.DueDay,
            IsEstimate = recurringExpense.IsEstimate,
            Archived = recurringExpense.Archived,
            Versions = recurringExpense.Versions
                .Append(newVersion)
                .OrderBy(version => version.ValidityStart)
                .Select(version => new ResponseRecurringExpenseVersionJson
                {
                    Id = version.Id,
                    RecurringExpenseId = version.RecurringExpenseId,
                    Amount = version.Amount,
                    ValidityStart = version.ValidityStart,
                    ValidityEnd = version.ValidityEnd,
                    ChangeReason = version.ChangeReason
                })
                .ToList()
        };
    }

    private static void Validate(RequestChangeRecurringExpenseValueJson request)
    {
        var result = new ChangeRecurringExpenseValueValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
