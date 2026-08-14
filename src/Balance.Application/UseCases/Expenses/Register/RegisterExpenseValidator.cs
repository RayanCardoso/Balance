using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Expenses.Register;

public class RegisterExpenseValidator : AbstractValidator<RequestRegisterExpenseJson>
{
    public RegisterExpenseValidator()
    {
        RuleFor(expense => expense.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);

        RuleFor(expense => expense.Amount)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
