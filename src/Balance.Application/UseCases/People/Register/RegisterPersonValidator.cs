using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.People.Register;

public class RegisterPersonValidator : AbstractValidator<RequestRegisterPersonJson>
{
    public RegisterPersonValidator()
    {
        RuleFor(person => person.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);
    }
}
