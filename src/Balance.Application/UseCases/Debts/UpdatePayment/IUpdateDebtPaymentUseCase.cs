using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Debts.UpdatePayment;

public interface IUpdateDebtPaymentUseCase
{
    Task<ResponseDebtPaymentJson> Execute(Guid id, RequestUpdateDebtPaymentJson request);
}
