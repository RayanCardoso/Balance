using Balance.Application.UseCases.Incomes.Register;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Incomes.Register;

public class RegisterIncomeSourceValidatorTest
{
    [Fact]
    public void Success_Recurring()
    {
        var result = new RegisterIncomeSourceValidator()
            .Validate(RequestRegisterIncomeSourceJsonBuilder.Recurring(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Variable()
    {
        var result = new RegisterIncomeSourceValidator()
            .Validate(RequestRegisterIncomeSourceJsonBuilder.Variable(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Recurring_Amount_Null()
    {
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(Guid.NewGuid());
        request.Amount = null;

        var result = new RegisterIncomeSourceValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void Error_Recurring_Expected_Day_Out_Of_Range(int expectedDay)
    {
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(Guid.NewGuid());
        request.ExpectedDay = expectedDay;

        var result = new RegisterIncomeSourceValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ResourceErrorMessages.EXPECTED_DAY_OUT_OF_RANGE);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Success_Recurring_Expected_Day_Boundaries(int expectedDay)
    {
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(Guid.NewGuid());
        request.ExpectedDay = expectedDay;

        var result = new RegisterIncomeSourceValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Variable_With_Amount()
    {
        var request = RequestRegisterIncomeSourceJsonBuilder.Variable(Guid.NewGuid());
        request.Amount = 100;

        var result = new RegisterIncomeSourceValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage
            .ShouldBe(ResourceErrorMessages.VARIABLE_SOURCE_HAS_NO_VERSION);
    }

    [Fact]
    public void Error_Variable_With_Expected_Day()
    {
        var request = RequestRegisterIncomeSourceJsonBuilder.Variable(Guid.NewGuid());
        request.ExpectedDay = 5;

        var result = new RegisterIncomeSourceValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage
            .ShouldBe(ResourceErrorMessages.VARIABLE_SOURCE_HAS_NO_VERSION);
    }
}
