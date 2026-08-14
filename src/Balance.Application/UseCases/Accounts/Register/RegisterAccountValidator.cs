using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Accounts.Register;

public class RegisterAccountValidator : AbstractValidator<RequestRegisterAccountJson>
{
    public RegisterAccountValidator()
    {
        RuleFor(account => account.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);

        RuleFor(account => account.ClosingDay!.Value)
            .InclusiveBetween(1, 31)
            .WithMessage(ResourceErrorMessages.DAY_OUT_OF_RANGE)
            .When(account => account.ClosingDay is not null);

        RuleFor(account => account.DueDay!.Value)
            .InclusiveBetween(1, 31)
            .WithMessage(ResourceErrorMessages.DAY_OUT_OF_RANGE)
            .When(account => account.DueDay is not null);
    }
}
