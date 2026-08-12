using Balance.Application.UseCases.Categories.Register;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Categories.Register;

public class RegisterCategoryValidatorTest
{
    [Fact]
    public void Success()
    {
        var result = new RegisterCategoryValidator().Validate(RequestRegisterCategoryJsonBuilder.Build());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Description_Null()
    {
        var request = RequestRegisterCategoryJsonBuilder.Build();
        request.Description = null;

        var result = new RegisterCategoryValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty()
    {
        var request = RequestRegisterCategoryJsonBuilder.Build();
        request.Name = string.Empty;

        var result = new RegisterCategoryValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ResourceErrorMessages.NAME_REQUIRED);
    }
}
