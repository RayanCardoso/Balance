using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.RecurringExpenses.UpdatePayment;

public interface IUpdateRecurringExpensePaymentUseCase
{
    Task<ResponseRecurringExpensePaymentJson> Execute(
        Guid paymentId, RequestUpdateRecurringExpensePaymentJson request);
}
