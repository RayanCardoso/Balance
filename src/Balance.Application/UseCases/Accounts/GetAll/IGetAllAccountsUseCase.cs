using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Accounts.GetAll;

public interface IGetAllAccountsUseCase
{
    Task<ResponseAccountsJson> Execute();
}
