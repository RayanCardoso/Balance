using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.RecurringExpenses.Register;

public class RegisterRecurringExpenseValidator : AbstractValidator<RequestRegisterRecurringExpenseJson>
{
    public RegisterRecurringExpenseValidator()
    {
        RuleFor(expense => expense.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);

        RuleFor(expense => expense.Amount)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        RuleFor(expense => expense.DueDay)
            .InclusiveBetween(1, 31)
            .WithMessage(ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }
}
