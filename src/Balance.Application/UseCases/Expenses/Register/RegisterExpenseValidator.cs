using Balance.Communication.Enums;
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

        // Only credit needs one: it is the account's closing day that decides which month the
        // purchase belongs to. A Pix does not come out of a card, and debit does not have to come
        // out of a registered account at all.
        RuleFor(expense => expense.AccountId)
            .NotNull()
            .When(expense => expense.Type == ExpenseType.Credit)
            .WithMessage(ResourceErrorMessages.ACCOUNT_REQUIRED_FOR_CREDIT);
    }
}
