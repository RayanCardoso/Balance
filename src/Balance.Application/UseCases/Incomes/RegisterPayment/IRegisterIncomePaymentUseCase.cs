using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Incomes.RegisterPayment;

public interface IRegisterIncomePaymentUseCase
{
    Task<ResponseIncomePaymentJson> Execute(RequestRegisterIncomePaymentJson request);
}
