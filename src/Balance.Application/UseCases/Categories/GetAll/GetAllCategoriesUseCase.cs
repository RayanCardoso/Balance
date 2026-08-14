using Balance.Communication.Responses;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Services.LoggedUser;
using CommunicationExpensePriority = Balance.Communication.Enums.ExpensePriority;

namespace Balance.Application.UseCases.Categories.GetAll;

public class GetAllCategoriesUseCase : IGetAllCategoriesUseCase
{
    private readonly ICategoryReadOnlyRepository _readOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetAllCategoriesUseCase(ICategoryReadOnlyRepository readOnlyRepository, ILoggedUser loggedUser)
    {
        _readOnlyRepository = readOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseCategoriesJson> Execute()
    {
        var loggedUser = await _loggedUser.Get();

        var categories = await _readOnlyRepository.GetAll(loggedUser);

        return new ResponseCategoriesJson
        {
            Categories = categories.Select(category => new ResponseCategoryJson
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Priority = (CommunicationExpensePriority)category.Priority
            }).ToList()
        };
    }
}
