using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Debts.GetById;

public interface IGetDebtByIdUseCase
{
    Task<ResponseDebtJson> Execute(Guid id);
}
