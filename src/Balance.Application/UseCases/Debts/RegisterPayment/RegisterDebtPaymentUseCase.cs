using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;
using DomainExpenseType = Balance.Domain.Enums.ExpenseType;

namespace Balance.Application.UseCases.Debts.RegisterPayment;

public class RegisterDebtPaymentUseCase : IRegisterDebtPaymentUseCase
{
    private readonly IDebtReadOnlyRepository _debtReadOnlyRepository;
    private readonly IDebtPaymentRepository _debtPaymentRepository;
    private readonly IAccountReadOnlyRepository _accountReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterDebtPaymentUseCase(
        IDebtReadOnlyRepository debtReadOnlyRepository,
        IDebtPaymentRepository debtPaymentRepository,
        IAccountReadOnlyRepository accountReadOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _debtReadOnlyRepository = debtReadOnlyRepository;
        _debtPaymentRepository = debtPaymentRepository;
        _accountReadOnlyRepository = accountReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseDebtPaymentJson> Execute(RequestRegisterDebtPaymentJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var debt = await _debtReadOnlyRepository.GetById(loggedUser, request.DebtId)
            ?? throw new NotFoundException(ResourceErrorMessages.DEBT_NOT_FOUND);

        if (debt.Archived)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.DEBT_ARCHIVED]);
        }

        // The branch below only tells whether the request supplied an installment id, never whether
        // it should have - a Scheduled debt without one moves the balance but produces no monthly
        // line (GetMonthlyDebtUseCase.BuildLines iterates only debt.Installments), and an OpenEnded
        // debt has no installment for the id to resolve against.
        if (debt.Mode == DebtMode.Scheduled && request.DebtInstallmentId is null)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.DEBT_INSTALLMENT_ID_REQUIRED]);
        }

        if (debt.Mode == DebtMode.OpenEnded && request.DebtInstallmentId is not null)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.DEBT_INSTALLMENT_ID_NOT_ALLOWED]);
        }

        DateOnly referenceMonth;

        if (request.DebtInstallmentId is { } installmentId)
        {
            // The id must resolve to an installment that actually hangs off this debt - an id that
            // belongs to a different debt is a 404, not a coincidence to be trusted.
            var installment = debt.Installments.FirstOrDefault(i => i.Id == installmentId)
                ?? throw new NotFoundException(ResourceErrorMessages.DEBT_INSTALLMENT_NOT_FOUND);

            // The unique index on DebtInstallmentId is defence in depth: this probe is what produces
            // the message, and the in-memory provider the tests run on ignores unique indexes.
            var alreadyRecorded = await _debtPaymentRepository.GetByInstallment(loggedUser, installmentId);

            if (alreadyRecorded is not null)
            {
                throw new ErrorOnValidationException([ResourceErrorMessages.PAYMENT_ALREADY_RECORDED]);
            }

            // Copied, never computed from the payment date - paying February's installment in March
            // is normal, and the payment still belongs to February.
            referenceMonth = installment.ReferenceMonth;
        }
        else
        {
            referenceMonth = request.PaymentDate.FirstDayOfMonth();
        }

        Account? account = null;

        if (request.AccountId is { } accountId)
        {
            account = await _accountReadOnlyRepository.GetById(loggedUser, accountId)
                ?? throw new NotFoundException(ResourceErrorMessages.ACCOUNT_NOT_FOUND);
        }

        var payment = new DebtPayment
        {
            DebtId = debt.Id,
            DebtInstallmentId = request.DebtInstallmentId,
            ReferenceMonth = referenceMonth,
            PaymentDate = request.PaymentDate,
            AmountPaid = request.AmountPaid,
            Type = (DomainExpenseType?)request.Type,
            AccountId = account?.Id,
            Notes = request.Notes
        };

        await _debtPaymentRepository.Add(payment);
        await _unitOfWork.Commit();

        return new ResponseDebtPaymentJson
        {
            Id = payment.Id,
            DebtId = payment.DebtId,
            DebtInstallmentId = payment.DebtInstallmentId,
            ReferenceMonth = payment.ReferenceMonth,
            PaymentDate = payment.PaymentDate,
            AmountPaid = payment.AmountPaid,
            Type = (CommunicationExpenseType?)payment.Type,
            AccountId = payment.AccountId,
            AccountName = account?.Name,
            Notes = payment.Notes
        };
    }

    private static void Validate(RequestRegisterDebtPaymentJson request)
    {
        var result = new RegisterDebtPaymentValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
