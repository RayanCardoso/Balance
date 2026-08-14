using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.RecurringExpenses.RegisterPayment;

public class RegisterRecurringExpensePaymentValidator
    : AbstractValidator<RequestRegisterRecurringExpensePaymentJson>
{
    public RegisterRecurringExpensePaymentValidator()
    {
        RuleFor(payment => payment.AmountPaid)
            .GreaterThan(0).WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
