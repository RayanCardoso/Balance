using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Debts.RegisterPayment;

public interface IRegisterDebtPaymentUseCase
{
    Task<ResponseDebtPaymentJson> Execute(RequestRegisterDebtPaymentJson request);
}
