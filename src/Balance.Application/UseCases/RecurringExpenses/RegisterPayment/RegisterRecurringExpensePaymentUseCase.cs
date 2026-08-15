using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;
using DomainExpenseType = Balance.Domain.Enums.ExpenseType;

namespace Balance.Application.UseCases.RecurringExpenses.RegisterPayment;

public class RegisterRecurringExpensePaymentUseCase : IRegisterRecurringExpensePaymentUseCase
{
    private readonly IRecurringExpenseReadOnlyRepository _recurringExpenseReadOnlyRepository;
    private readonly IRecurringExpensePaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterRecurringExpensePaymentUseCase(
        IRecurringExpenseReadOnlyRepository recurringExpenseReadOnlyRepository,
        IRecurringExpensePaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _recurringExpenseReadOnlyRepository = recurringExpenseReadOnlyRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseRecurringExpensePaymentJson> Execute(
        RequestRegisterRecurringExpensePaymentJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var recurringExpense = await _recurringExpenseReadOnlyRepository
            .GetById(loggedUser, request.RecurringExpenseId)
            ?? throw new NotFoundException(ResourceErrorMessages.RECURRING_EXPENSE_NOT_FOUND);

        if (recurringExpense.Archived)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.RECURRING_EXPENSE_ARCHIVED]);
        }

        var referenceMonth = request.ReferenceMonth.FirstDayOfMonth();

        // The unique index on (RecurringExpenseId, ReferenceMonth) is defence in depth: this probe
        // is what produces the message, and the in-memory provider the tests run on ignores indexes.
        var alreadyRecorded = await _paymentRepository.GetByMonth(recurringExpense.Id, referenceMonth);

        if (alreadyRecorded is not null)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.PAYMENT_ALREADY_RECORDED]);
        }

        // The version in effect at the reference month, not the one currently open. Recording a
        // payment for a past month after a value change must freeze the amount that month really had.
        var version = recurringExpense.VersionInEffect(referenceMonth)
            ?? throw new ErrorOnValidationException([ResourceErrorMessages.NO_VERSION_IN_EFFECT]);

        var payment = new RecurringExpensePayment
        {
            RecurringExpenseId = recurringExpense.Id,
            RecurringExpenseVersionId = version.Id,
            ReferenceMonth = referenceMonth,
            PaymentDate = request.PaymentDate,
            AmountPaid = request.AmountPaid,
            Notes = request.Notes,
            AccountId = request.AccountId,
            Type = (DomainExpenseType?)request.Type
        };

        await _paymentRepository.Add(payment);
        await _unitOfWork.Commit();

        return new ResponseRecurringExpensePaymentJson
        {
            Id = payment.Id,
            RecurringExpenseId = payment.RecurringExpenseId,
            RecurringExpenseVersionId = payment.RecurringExpenseVersionId,
            ReferenceMonth = payment.ReferenceMonth,
            PaymentDate = payment.PaymentDate,
            AmountPaid = payment.AmountPaid,
            Notes = payment.Notes,
            AccountId = payment.AccountId,
            Type = (CommunicationExpenseType?)payment.Type
        };
    }

    private static void Validate(RequestRegisterRecurringExpensePaymentJson request)
    {
        var result = new RegisterRecurringExpensePaymentValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
