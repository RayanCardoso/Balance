using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Incomes.Register;

public interface IRegisterIncomeSourceUseCase
{
    Task<ResponseIncomeSourceJson> Execute(RequestRegisterIncomeSourceJson request);
}
