using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Repositories.Expenses;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Expenses.Register;

// SPEC_DEVIATION: the design lists PERSON_NOT_FOUND as the only reused not-found key, leaving a
// foreign category or account with no message of its own.
// Reason: a 404 body reading "Person not found." when the category is the foreign one misleads the
// caller. CATEGORY_NOT_FOUND and ACCOUNT_NOT_FOUND follow the existing INCOME_SOURCE_NOT_FOUND
// convention. The status code the spec pins (404, AD-004) is unchanged.
public class RegisterExpenseUseCase : IRegisterExpenseUseCase
{
    private readonly IExpenseWriteOnlyRepository _writeOnlyRepository;
    private readonly IPersonReadOnlyRepository _personReadOnlyRepository;
    private readonly ICategoryReadOnlyRepository _categoryReadOnlyRepository;
    private readonly IAccountReadOnlyRepository _accountReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterExpenseUseCase(
        IExpenseWriteOnlyRepository writeOnlyRepository,
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

    public async Task<ResponseExpenseJson> Execute(RequestRegisterExpenseJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var person = await _personReadOnlyRepository.GetById(loggedUser, request.PersonId)
            ?? throw new NotFoundException(ResourceErrorMessages.PERSON_NOT_FOUND);

        var category = await _categoryReadOnlyRepository.GetById(loggedUser, request.CategoryId)
            ?? throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);

        // The account may belong to a different Person of the same user (EXPN-01 AC7).
        var account = await _accountReadOnlyRepository.GetById(loggedUser, request.AccountId)
            ?? throw new NotFoundException(ResourceErrorMessages.ACCOUNT_NOT_FOUND);

        var type = (ExpenseType)request.Type;

        var expense = new Expense
        {
            Name = request.Name,
            Type = type,
            Amount = request.Amount,
            Date = request.Date,
            CompetenceMonth = request.CompetenceMonth?.FirstDayOfMonth()
                ?? CompetenceMonthResolver.Resolve(type, account.ClosingDay, request.Date),
            PersonId = person.Id,
            CategoryId = category.Id,
            AccountId = account.Id
        };

        await _writeOnlyRepository.Add(expense);
        await _unitOfWork.Commit();

        return new ResponseExpenseJson
        {
            Id = expense.Id,
            Name = expense.Name,
            PersonId = expense.PersonId,
            Type = request.Type,
            Amount = expense.Amount,
            CategoryId = expense.CategoryId,
            AccountId = expense.AccountId,
            Date = expense.Date,
            CompetenceMonth = expense.CompetenceMonth,
            InstallmentNumber = expense.InstallmentNumber,
            InstallmentPlanId = expense.InstallmentPlanId
        };
    }

    private static void Validate(RequestRegisterExpenseJson request)
    {
        var result = new RegisterExpenseValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
