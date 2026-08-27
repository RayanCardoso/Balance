using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;
using DomainExpenseType = Balance.Domain.Enums.ExpenseType;

namespace Balance.Application.UseCases.Debts.UpdatePayment;

public class UpdateDebtPaymentUseCase : IUpdateDebtPaymentUseCase
{
    private readonly IDebtPaymentRepository _debtPaymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public UpdateDebtPaymentUseCase(
        IDebtPaymentRepository debtPaymentRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _debtPaymentRepository = debtPaymentRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseDebtPaymentJson> Execute(Guid id, RequestUpdateDebtPaymentJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var payment = await _debtPaymentRepository.GetById(loggedUser, id)
            ?? throw new NotFoundException(ResourceErrorMessages.DEBT_PAYMENT_NOT_FOUND);

        // ReferenceMonth, DebtId and DebtInstallmentId are deliberately left alone: a correction
        // changes what was paid, never which month, debt or installment it belongs to. Rewriting any
        // of those would rewrite recorded history.
        payment.AmountPaid = request.AmountPaid;
        payment.PaymentDate = request.PaymentDate;
        payment.Type = (DomainExpenseType?)request.Type;
        payment.AccountId = request.AccountId;
        payment.Notes = request.Notes;

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
            Notes = payment.Notes
        };
    }

    private static void Validate(RequestUpdateDebtPaymentJson request)
    {
        var result = new UpdateDebtPaymentValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
