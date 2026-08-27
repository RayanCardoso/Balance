using Balance.Communication.Enums;
using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Debts.UpdatePayment;

public class UpdateDebtPaymentValidator : AbstractValidator<RequestUpdateDebtPaymentJson>
{
    public UpdateDebtPaymentValidator()
    {
        RuleFor(payment => payment.AmountPaid)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        // A correction can move a Credit payment away from Pix/Debit just as easily as a first
        // registration can - the same rule about needing an account applies here too.
        RuleFor(payment => payment.AccountId)
            .NotNull()
            .When(payment => payment.Type == ExpenseType.Credit)
            .WithMessage(ResourceErrorMessages.ACCOUNT_REQUIRED_FOR_CREDIT);
    }
}
