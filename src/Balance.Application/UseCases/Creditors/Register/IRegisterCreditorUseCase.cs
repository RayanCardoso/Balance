using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Creditors.Register;

public interface IRegisterCreditorUseCase
{
    Task<ResponseCreditorJson> Execute(RequestRegisterCreditorJson request);
}
