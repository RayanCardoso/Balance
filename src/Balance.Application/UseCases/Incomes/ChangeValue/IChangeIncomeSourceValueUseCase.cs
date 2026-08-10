using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Incomes.ChangeValue;

public interface IChangeIncomeSourceValueUseCase
{
    Task<ResponseIncomeSourceVersionJson> Execute(RequestChangeIncomeSourceValueJson request);
}
