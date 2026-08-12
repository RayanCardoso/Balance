using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Accounts.Register;

public interface IRegisterAccountUseCase
{
    Task<ResponseAccountJson> Execute(RequestRegisterAccountJson request);
}
