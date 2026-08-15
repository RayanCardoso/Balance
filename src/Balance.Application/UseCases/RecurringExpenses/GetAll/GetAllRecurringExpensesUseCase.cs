using Balance.Communication.Responses;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace Balance.Application.UseCases.RecurringExpenses.GetAll;

public class GetAllRecurringExpensesUseCase : IGetAllRecurringExpensesUseCase
{
    private readonly IRecurringExpenseReadOnlyRepository _readOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetAllRecurringExpensesUseCase(
        IRecurringExpenseReadOnlyRepository readOnlyRepository,
        ILoggedUser loggedUser)
    {
        _readOnlyRepository = readOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseRecurringExpensesJson> Execute()
    {
        var loggedUser = await _loggedUser.Get();

        var recurringExpenses = await _readOnlyRepository.GetAll(loggedUser);

        return new ResponseRecurringExpensesJson
        {
            RecurringExpenses = recurringExpenses.Select(expense => new ResponseRecurringExpenseJson
            {
                Id = expense.Id,
                Name = expense.Name,
                PersonId = expense.PersonId,
                Type = (CommunicationExpenseType)expense.Type,
                CategoryId = expense.CategoryId,
                AccountId = expense.AccountId,
                DueDay = expense.DueDay,
                IsEstimate = expense.IsEstimate,
                Archived = expense.Archived,
                Versions = expense.Versions.Select(version => new ResponseRecurringExpenseVersionJson
                {
                    Id = version.Id,
                    RecurringExpenseId = version.RecurringExpenseId,
                    Amount = version.Amount,
                    ValidityStart = version.ValidityStart,
                    ValidityEnd = version.ValidityEnd,
                    ChangeReason = version.ChangeReason
                }).ToList()
            }).ToList()
        };
    }
}
