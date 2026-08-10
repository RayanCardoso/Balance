using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Incomes;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using DomainIncomeType = Balance.Domain.Enums.IncomeType;

namespace Balance.Application.UseCases.Incomes.Register;

public class RegisterIncomeSourceUseCase : IRegisterIncomeSourceUseCase
{
    private readonly IIncomeSourceWriteOnlyRepository _writeOnlyRepository;
    private readonly IPersonReadOnlyRepository _personReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterIncomeSourceUseCase(
        IIncomeSourceWriteOnlyRepository writeOnlyRepository,
        IPersonReadOnlyRepository personReadOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _writeOnlyRepository = writeOnlyRepository;
        _personReadOnlyRepository = personReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseIncomeSourceJson> Execute(RequestRegisterIncomeSourceJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var person = await _personReadOnlyRepository.GetById(loggedUser, request.PersonId)
            ?? throw new NotFoundException(ResourceErrorMessages.PERSON_NOT_FOUND);

        var incomeSource = new IncomeSource
        {
            Name = request.Name,
            Type = (DomainIncomeType)request.Type,
            PersonId = person.Id,
            Archived = false
        };

        await _writeOnlyRepository.Add(incomeSource);

        IncomeSourceVersion? version = null;

        if (incomeSource.Type == DomainIncomeType.Recurring)
        {
            version = new IncomeSourceVersion
            {
                IncomeSourceId = incomeSource.Id,
                Amount = request.Amount!.Value,
                ExpectedDay = request.ExpectedDay!.Value,
                ValidityStart = request.ValidityStart ?? DateOnly.FromDateTime(DateTime.UtcNow),
                ValidityEnd = null,
                ChangeReason = request.ChangeReason ?? string.Empty
            };

            await _writeOnlyRepository.AddVersion(version);
        }

        await _unitOfWork.Commit();

        return new ResponseIncomeSourceJson
        {
            Id = incomeSource.Id,
            Name = incomeSource.Name,
            Type = request.Type,
            PersonId = incomeSource.PersonId,
            Archived = incomeSource.Archived,
            Amount = version?.Amount,
            ExpectedDay = version?.ExpectedDay
        };
    }

    private static void Validate(RequestRegisterIncomeSourceJson request)
    {
        var result = new RegisterIncomeSourceValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
