using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.RecurringExpenses.Register;

public interface IRegisterRecurringExpenseUseCase
{
    Task<ResponseRecurringExpenseJson> Execute(RequestRegisterRecurringExpenseJson request);
}
