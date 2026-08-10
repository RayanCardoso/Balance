using Balance.Application.UseCases.People.Register;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.People.Register;

public class RegisterPersonValidatorTest
{
    [Fact]
    public void Success()
    {
        var validator = new RegisterPersonValidator();
        var request = RequestRegisterPersonJsonBuilder.Build();

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Description_Null()
    {
        var validator = new RegisterPersonValidator();
        var request = RequestRegisterPersonJsonBuilder.Build();
        request.Description = null;

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty()
    {
        var validator = new RegisterPersonValidator();
        var request = RequestRegisterPersonJsonBuilder.Build();
        request.Name = string.Empty;

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ResourceErrorMessages.NAME_REQUIRED);
    }
}
