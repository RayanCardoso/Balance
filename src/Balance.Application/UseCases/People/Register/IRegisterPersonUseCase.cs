using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.People.Register;

public interface IRegisterPersonUseCase
{
    Task<ResponsePersonJson> Execute(RequestRegisterPersonJson request);
}
