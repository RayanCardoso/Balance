using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Incomes;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Incomes.RegisterPayment;

public class RegisterIncomePaymentUseCase : IRegisterIncomePaymentUseCase
{
    private readonly IIncomeSourceReadOnlyRepository _incomeSourceReadOnlyRepository;
    private readonly IIncomePaymentWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterIncomePaymentUseCase(
        IIncomeSourceReadOnlyRepository incomeSourceReadOnlyRepository,
        IIncomePaymentWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _incomeSourceReadOnlyRepository = incomeSourceReadOnlyRepository;
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseIncomePaymentJson> Execute(RequestRegisterIncomePaymentJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var incomeSource = await _incomeSourceReadOnlyRepository.GetById(loggedUser, request.IncomeSourceId)
            ?? throw new NotFoundException(ResourceErrorMessages.INCOME_SOURCE_NOT_FOUND);

        if (incomeSource.Archived)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.INCOME_SOURCE_ARCHIVED]);
        }

        var referenceMonth = request.ReferenceMonth.FirstDayOfMonth();

        Guid? versionId = null;

        if (incomeSource.Type == IncomeType.Recurring)
        {
            var version = incomeSource.VersionInEffect(referenceMonth)
                ?? throw new ErrorOnValidationException([ResourceErrorMessages.NO_VERSION_IN_EFFECT]);

            versionId = version.Id;
        }

        var payment = new IncomePayment
        {
            IncomeSourceId = incomeSource.Id,
            IncomeSourceVersionId = versionId,
            PaymentDate = request.PaymentDate,
            ReferenceMonth = referenceMonth,
            AmountReceived = request.AmountReceived,
            Notes = request.Notes
        };

        await _writeOnlyRepository.Add(payment);
        await _unitOfWork.Commit();

        return new ResponseIncomePaymentJson
        {
            Id = payment.Id,
            IncomeSourceId = payment.IncomeSourceId,
            IncomeSourceVersionId = payment.IncomeSourceVersionId,
            PaymentDate = payment.PaymentDate,
            ReferenceMonth = payment.ReferenceMonth,
            AmountReceived = payment.AmountReceived,
            Notes = payment.Notes
        };
    }

    private static void Validate(RequestRegisterIncomePaymentJson request)
    {
        var result = new RegisterIncomePaymentValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
