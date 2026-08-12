using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Categories.GetAll;

public interface IGetAllCategoriesUseCase
{
    Task<ResponseCategoriesJson> Execute();
}
