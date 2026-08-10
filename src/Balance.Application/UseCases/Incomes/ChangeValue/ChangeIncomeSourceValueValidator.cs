using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Incomes.ChangeValue;

public class ChangeIncomeSourceValueValidator : AbstractValidator<RequestChangeIncomeSourceValueJson>
{
    public ChangeIncomeSourceValueValidator()
    {
        RuleFor(change => change.Amount)
            .GreaterThan(0).WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        RuleFor(change => change.ExpectedDay)
            .InclusiveBetween(1, 31).WithMessage(ResourceErrorMessages.EXPECTED_DAY_OUT_OF_RANGE);

        RuleFor(change => change.ChangeReason)
            .NotEmpty().WithMessage(ResourceErrorMessages.CHANGE_REASON_REQUIRED);
    }
}
