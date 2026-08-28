using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Creditors.Register;

public class RegisterCreditorValidator : AbstractValidator<RequestRegisterCreditorJson>
{
    public RegisterCreditorValidator()
    {
        RuleFor(creditor => creditor.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);
    }
}
