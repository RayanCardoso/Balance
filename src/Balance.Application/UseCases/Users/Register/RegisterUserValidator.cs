using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Users.Register;

public class RegisterUserValidator : AbstractValidator<RequestRegisterUserJson>
{
    public RegisterUserValidator()
    {
        RuleFor(user => user.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);
        RuleFor(user => user.Email).NotEmpty().WithMessage(ResourceErrorMessages.EMAIL_REQUIRED);
        RuleFor(user => user.Password).NotEmpty().WithMessage(ResourceErrorMessages.PASSWORD_REQUIRED);
        RuleFor(user => user.Password).MinimumLength(6).WithMessage(ResourceErrorMessages.PASSWORD_TOO_SHORT);

        When(user => string.IsNullOrWhiteSpace(user.Email) == false, () =>
        {
            RuleFor(user => user.Email).EmailAddress().WithMessage(ResourceErrorMessages.EMAIL_INVALID);
        });
    }
}
