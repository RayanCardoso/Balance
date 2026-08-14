using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;

public class RegisterInstallmentPlanValidator : AbstractValidator<RequestRegisterInstallmentPlanJson>
{
    public RegisterInstallmentPlanValidator()
    {
        RuleFor(plan => plan.TotalAmount)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        RuleFor(plan => plan.InstallmentCount)
            .GreaterThanOrEqualTo(2)
            .WithMessage(ResourceErrorMessages.INSTALLMENT_COUNT_INVALID);
    }
}
