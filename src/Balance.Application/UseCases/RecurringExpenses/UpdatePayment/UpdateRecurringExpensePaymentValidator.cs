using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.RecurringExpenses.UpdatePayment;

public class UpdateRecurringExpensePaymentValidator
    : AbstractValidator<RequestUpdateRecurringExpensePaymentJson>
{
    public UpdateRecurringExpensePaymentValidator()
    {
        RuleFor(payment => payment.AmountPaid)
            .GreaterThan(0).WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
