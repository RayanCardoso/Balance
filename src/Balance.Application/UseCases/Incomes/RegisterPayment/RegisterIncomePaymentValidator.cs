using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Incomes.RegisterPayment;

public class RegisterIncomePaymentValidator : AbstractValidator<RequestRegisterIncomePaymentJson>
{
    public RegisterIncomePaymentValidator()
    {
        RuleFor(payment => payment.AmountReceived)
            .GreaterThan(0).WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
