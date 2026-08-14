using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Categories.Register;

public interface IRegisterCategoryUseCase
{
    Task<ResponseCategoryJson> Execute(RequestRegisterCategoryJson request);
}
