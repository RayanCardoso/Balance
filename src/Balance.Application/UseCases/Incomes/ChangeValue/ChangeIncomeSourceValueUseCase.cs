using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Incomes;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Incomes.ChangeValue;

public class ChangeIncomeSourceValueUseCase : IChangeIncomeSourceValueUseCase
{
    private readonly IIncomeSourceUpdateOnlyRepository _updateOnlyRepository;
    private readonly IIncomeSourceWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public ChangeIncomeSourceValueUseCase(
        IIncomeSourceUpdateOnlyRepository updateOnlyRepository,
        IIncomeSourceWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _updateOnlyRepository = updateOnlyRepository;
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseIncomeSourceVersionJson> Execute(RequestChangeIncomeSourceValueJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var incomeSource = await _updateOnlyRepository.GetById(loggedUser, request.IncomeSourceId)
            ?? throw new NotFoundException(ResourceErrorMessages.INCOME_SOURCE_NOT_FOUND);

        if (incomeSource.Type != IncomeType.Recurring)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.VARIABLE_SOURCE_HAS_NO_VERSION]);
        }

        var currentVersion = incomeSource.Versions.SingleOrDefault(version => version.ValidityEnd is null)
            ?? throw new ErrorOnValidationException([ResourceErrorMessages.NO_VERSION_IN_EFFECT]);

        if (request.ValidityStart <= currentVersion.ValidityStart)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.VALIDITY_START_MUST_BE_LATER]);
        }

        currentVersion.ValidityEnd = request.ValidityStart.AddDays(-1);

        var newVersion = new IncomeSourceVersion
        {
            IncomeSourceId = incomeSource.Id,
            Amount = request.Amount,
            ExpectedDay = request.ExpectedDay,
            ValidityStart = request.ValidityStart,
            ValidityEnd = null,
            ChangeReason = request.ChangeReason
        };

        await _writeOnlyRepository.AddVersion(newVersion);
        await _unitOfWork.Commit();

        return new ResponseIncomeSourceVersionJson
        {
            Id = newVersion.Id,
            IncomeSourceId = newVersion.IncomeSourceId,
            Amount = newVersion.Amount,
            ExpectedDay = newVersion.ExpectedDay,
            ValidityStart = newVersion.ValidityStart,
            ValidityEnd = newVersion.ValidityEnd,
            ChangeReason = newVersion.ChangeReason
        };
    }

    private static void Validate(RequestChangeIncomeSourceValueJson request)
    {
        var result = new ChangeIncomeSourceValueValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
