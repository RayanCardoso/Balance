using Balance.Communication.Enums;
using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Debts.Register;

public class RegisterDebtValidator : AbstractValidator<RequestRegisterDebtJson>
{
    public RegisterDebtValidator()
    {
        RuleFor(debt => debt.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);

        RuleFor(debt => debt.PrincipalAmount)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        RuleFor(debt => debt.TotalAmount)
            .GreaterThan(0)
            .WithMessage(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        RuleFor(debt => debt.TotalAmount)
            .GreaterThanOrEqualTo(debt => debt.PrincipalAmount)
            .WithMessage(ResourceErrorMessages.TOTAL_LESS_THAN_PRINCIPAL);

        // A scheduled debt has no term without both. Once both are present, each is checked on its
        // own terms - a bad due day should not be masked by reporting a missing schedule instead.
        When(debt => debt.Mode == DebtMode.Scheduled, () =>
        {
            RuleFor(debt => debt)
                .Must(debt => debt.InstallmentCount is not null && debt.DueDay is not null)
                .WithMessage(ResourceErrorMessages.SCHEDULE_REQUIRED);

            RuleFor(debt => debt.InstallmentCount!.Value)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ResourceErrorMessages.DEBT_INSTALLMENT_COUNT_INVALID)
                .When(debt => debt.InstallmentCount is not null);

            RuleFor(debt => debt.DueDay!.Value)
                .InclusiveBetween(1, 31)
                .WithMessage(ResourceErrorMessages.DAY_OUT_OF_RANGE)
                .When(debt => debt.DueDay is not null);
        });

        // An open-ended debt has no term at all - either field being supplied would contradict that.
        RuleFor(debt => debt)
            .Must(debt => debt.InstallmentCount is null && debt.DueDay is null)
            .WithMessage(ResourceErrorMessages.SCHEDULE_NOT_ALLOWED)
            .When(debt => debt.Mode == DebtMode.OpenEnded);
    }
}
