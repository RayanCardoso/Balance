using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.RecurringExpenses.RegisterPayment;

public interface IRegisterRecurringExpensePaymentUseCase
{
    Task<ResponseRecurringExpensePaymentJson> Execute(RequestRegisterRecurringExpensePaymentJson request);
}
