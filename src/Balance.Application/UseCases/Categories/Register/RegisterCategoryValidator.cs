using Balance.Communication.Requests;
using Balance.Exception;
using FluentValidation;

namespace Balance.Application.UseCases.Categories.Register;

public class RegisterCategoryValidator : AbstractValidator<RequestRegisterCategoryJson>
{
    public RegisterCategoryValidator()
    {
        RuleFor(category => category.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);
    }
}
