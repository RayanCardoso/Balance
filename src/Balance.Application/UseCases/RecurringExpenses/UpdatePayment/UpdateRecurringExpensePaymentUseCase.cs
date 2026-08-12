using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.RecurringExpenses.UpdatePayment;

// SPEC_DEVIATION: the design's error table names no not-found key for a recorded payment.
// Reason: reusing RECURRING_EXPENSE_NOT_FOUND would answer a missing payment with a body naming
// the wrong entity. RECURRING_EXPENSE_PAYMENT_NOT_FOUND follows the correction T18 and T28 applied.
// The 404 status AD-004 pins is unchanged.
public class UpdateRecurringExpensePaymentUseCase : IUpdateRecurringExpensePaymentUseCase
{
    private readonly IRecurringExpensePaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public UpdateRecurringExpensePaymentUseCase(
        IRecurringExpensePaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseRecurringExpensePaymentJson> Execute(
        Guid paymentId, RequestUpdateRecurringExpensePaymentJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var payment = await _paymentRepository.GetById(loggedUser, paymentId)
            ?? throw new NotFoundException(ResourceErrorMessages.RECURRING_EXPENSE_PAYMENT_NOT_FOUND);

        // ReferenceMonth and RecurringExpenseVersionId are deliberately left alone: a correction
        // changes what was paid, never which month it belongs to nor which version it was measured
        // against. Recomputing the version here would rewrite recorded history.
        payment.AmountPaid = request.AmountPaid;
        payment.PaymentDate = request.PaymentDate;
        payment.Notes = request.Notes;
        payment.AccountId = request.AccountId;

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
            AccountId = payment.AccountId
        };
    }

    private static void Validate(RequestUpdateRecurringExpensePaymentJson request)
    {
        var result = new UpdateRecurringExpensePaymentValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
