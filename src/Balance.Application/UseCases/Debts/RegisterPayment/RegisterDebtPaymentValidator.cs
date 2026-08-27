using Balance.Communication.Enums;
using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Debts.RegisterPayment;

public class RegisterDebtPaymentValidator : AbstractValidator<RequestRegisterDebtPaymentJson>
{
    public RegisterDebtPaymentValidator()
    {
        RuleFor(payment => payment.AmountPaid)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        // Only credit needs one: a Pix does not come out of a card, and debit does not have to
        // come out of a registered account at all.
        RuleFor(payment => payment.AccountId)
            .NotNull()
            .When(payment => payment.Type == ExpenseType.Credit)
            .WithMessage(ResourceErrorMessages.ACCOUNT_REQUIRED_FOR_CREDIT);
    }
}
