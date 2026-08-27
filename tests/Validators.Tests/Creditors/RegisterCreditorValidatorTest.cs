using System.Globalization;
using Balance.Application.UseCases.Creditors.Register;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Creditors;

public class RegisterCreditorValidatorTest
{
    [Fact]
    public void Success()
    {
        var result = new RegisterCreditorValidator().Validate(RequestRegisterCreditorJsonBuilder.Build());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Contact_Null()
    {
        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Contact = null;

        var result = new RegisterCreditorValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Notes_Null()
    {
        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Notes = null;

        var result = new RegisterCreditorValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty_Invariant_Culture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            var request = RequestRegisterCreditorJsonBuilder.Build();
            request.Name = string.Empty;

            var result = new RegisterCreditorValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ResourceErrorMessages.NAME_REQUIRED);
            result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Name is required.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Error_Name_Empty_PtBr_Culture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");

        try
        {
            var request = RequestRegisterCreditorJsonBuilder.Build();
            request.Name = string.Empty;

            var result = new RegisterCreditorValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ResourceErrorMessages.NAME_REQUIRED);
            result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("O nome é obrigatório.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
