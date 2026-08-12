using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception.ExceptionBase;
using DomainExpensePriority = Balance.Domain.Enums.ExpensePriority;

namespace Balance.Application.UseCases.Categories.Register;

public class RegisterCategoryUseCase : IRegisterCategoryUseCase
{
    private readonly ICategoryWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterCategoryUseCase(
        ICategoryWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseCategoryJson> Execute(RequestRegisterCategoryJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            Priority = (DomainExpensePriority)request.Priority,
            UserId = loggedUser.Id
        };

        await _writeOnlyRepository.Add(category);
        await _unitOfWork.Commit();

        return new ResponseCategoryJson
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Priority = request.Priority
        };
    }

    private static void Validate(RequestRegisterCategoryJson request)
    {
        var result = new RegisterCategoryValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
