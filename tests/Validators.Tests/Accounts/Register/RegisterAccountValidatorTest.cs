using Balance.Application.UseCases.Accounts.Register;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Accounts.Register;

public class RegisterAccountValidatorTest
{
    [Fact]
    public void Success()
    {
        var result = new RegisterAccountValidator().Validate(RequestRegisterAccountJsonBuilder.Build(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Debit_Account_With_No_Card_Fields()
    {
        var request = RequestRegisterAccountJsonBuilder.Debit(Guid.NewGuid());

        var result = new RegisterAccountValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty()
    {
        var request = RequestRegisterAccountJsonBuilder.Build(Guid.NewGuid());
        request.Name = string.Empty;

        var result = new RegisterAccountValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.NAME_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void Error_Closing_Day_Out_Of_Range(int closingDay)
    {
        var request = RequestRegisterAccountJsonBuilder.Build(Guid.NewGuid());
        request.ClosingDay = closingDay;

        var result = new RegisterAccountValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void Error_Due_Day_Out_Of_Range(int dueDay)
    {
        var request = RequestRegisterAccountJsonBuilder.Build(Guid.NewGuid());
        request.DueDay = dueDay;

        var result = new RegisterAccountValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Success_Day_Boundaries(int day)
    {
        var request = RequestRegisterAccountJsonBuilder.Build(Guid.NewGuid());
        request.ClosingDay = day;
        request.DueDay = day;

        var result = new RegisterAccountValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
