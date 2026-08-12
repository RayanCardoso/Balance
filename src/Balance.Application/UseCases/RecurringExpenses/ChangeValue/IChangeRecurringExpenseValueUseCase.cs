using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.RecurringExpenses.ChangeValue;

public interface IChangeRecurringExpenseValueUseCase
{
    Task<ResponseRecurringExpenseJson> Execute(RequestChangeRecurringExpenseValueJson request);
}
