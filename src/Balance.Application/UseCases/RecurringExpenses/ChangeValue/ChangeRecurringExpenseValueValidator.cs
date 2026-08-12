using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.RecurringExpenses.ChangeValue;

public class ChangeRecurringExpenseValueValidator : AbstractValidator<RequestChangeRecurringExpenseValueJson>
{
    public ChangeRecurringExpenseValueValidator()
    {
        RuleFor(change => change.ChangeReason)
            .NotEmpty()
            .WithMessage(ResourceErrorMessages.CHANGE_REASON_REQUIRED);
    }
}
