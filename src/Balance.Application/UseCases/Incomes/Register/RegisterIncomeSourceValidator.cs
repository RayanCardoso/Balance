using Balance.Communication.Enums;
using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Incomes.Register;

public class RegisterIncomeSourceValidator : AbstractValidator<RequestRegisterIncomeSourceJson>
{
    public RegisterIncomeSourceValidator()
    {
        RuleFor(source => source.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);

        When(source => source.Type == IncomeType.Recurring, () =>
        {
            RuleFor(source => source.Amount)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO)
                .GreaterThan(0).WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

            RuleFor(source => source.ExpectedDay)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ResourceErrorMessages.EXPECTED_DAY_OUT_OF_RANGE)
                .InclusiveBetween(1, 31).WithMessage(ResourceErrorMessages.EXPECTED_DAY_OUT_OF_RANGE);
        });
    }
}
