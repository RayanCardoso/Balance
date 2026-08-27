using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Debts.Register;

public interface IRegisterDebtUseCase
{
    Task<ResponseDebtJson> Execute(RequestRegisterDebtJson request);
}
