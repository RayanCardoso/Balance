using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Repositories.People;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.RecurringExpenses.Register;

public class RegisterRecurringExpenseUseCase : IRegisterRecurringExpenseUseCase
{
    private readonly IRecurringExpenseWriteOnlyRepository _writeOnlyRepository;
    private readonly IPersonReadOnlyRepository _personReadOnlyRepository;
    private readonly ICategoryReadOnlyRepository _categoryReadOnlyRepository;
    private readonly IAccountReadOnlyRepository _accountReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterRecurringExpenseUseCase(
        IRecurringExpenseWriteOnlyRepository writeOnlyRepository,
        IPersonReadOnlyRepository personReadOnlyRepository,
        ICategoryReadOnlyRepository categoryReadOnlyRepository,
        IAccountReadOnlyRepository accountReadOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _writeOnlyRepository = writeOnlyRepository;
        _personReadOnlyRepository = personReadOnlyRepository;
        _categoryReadOnlyRepository = categoryReadOnlyRepository;
        _accountReadOnlyRepository = accountReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseRecurringExpenseJson> Execute(RequestRegisterRecurringExpenseJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var person = await _personReadOnlyRepository.GetById(loggedUser, request.PersonId)
            ?? throw new NotFoundException(ResourceErrorMessages.PERSON_NOT_FOUND);

        var category = await _categoryReadOnlyRepository.GetById(loggedUser, request.CategoryId)
            ?? throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);

        Guid? accountId = null;

        if(request.AccountId != null)
        {
            var account = await _accountReadOnlyRepository.GetById(loggedUser, request.AccountId.Value)
                ?? throw new NotFoundException(ResourceErrorMessages.ACCOUNT_NOT_FOUND);

            accountId = account.Id;
        }

        var recurringExpense = new RecurringExpense
        {
            Name = request.Name,
            Type = (ExpenseType)request.Type,
            DueDay = request.DueDay,
            IsEstimate = request.IsEstimate,
            Archived = false,
            PersonId = person.Id,
            CategoryId = category.Id,
            AccountId = accountId
        };

        await _writeOnlyRepository.Add(recurringExpense);

        var version = new RecurringExpenseVersion
        {
            RecurringExpenseId = recurringExpense.Id,
            Amount = request.Amount,
            ValidityStart = request.ValidityStart ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityEnd = null,
            ChangeReason = request.ChangeReason ?? string.Empty
        };

        await _writeOnlyRepository.AddVersion(version);

        await _unitOfWork.Commit();

        return new ResponseRecurringExpenseJson
        {
            Id = recurringExpense.Id,
            Name = recurringExpense.Name,
            PersonId = recurringExpense.PersonId,
            Type = request.Type,
            CategoryId = recurringExpense.CategoryId,
            AccountId = accountId,
            DueDay = recurringExpense.DueDay,
            IsEstimate = recurringExpense.IsEstimate,
            Archived = recurringExpense.Archived,
            Versions =
            [
                new ResponseRecurringExpenseVersionJson
                {
                    Id = version.Id,
                    RecurringExpenseId = version.RecurringExpenseId,
                    Amount = version.Amount,
                    ValidityStart = version.ValidityStart,
                    ValidityEnd = version.ValidityEnd,
                    ChangeReason = version.ChangeReason
                }
            ]
        };
    }

    private static void Validate(RequestRegisterRecurringExpenseJson request)
    {
        var result = new RegisterRecurringExpenseValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
