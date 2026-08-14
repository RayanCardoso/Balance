using Balance.Communication.Responses;

namespace Balance.Application.UseCases.RecurringExpenses.GetAll;

public interface IGetAllRecurringExpensesUseCase
{
    Task<ResponseRecurringExpensesJson> Execute();
}
