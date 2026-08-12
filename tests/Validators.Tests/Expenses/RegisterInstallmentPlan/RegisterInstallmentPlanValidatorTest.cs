using Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Expenses.RegisterInstallmentPlan;

public class RegisterInstallmentPlanValidatorTest
{
    [Fact]
    public void Success()
    {
        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = new RegisterInstallmentPlanValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_At_The_Minimum_Installment_Count()
    {
        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.InstallmentCount = 2;

        var result = new RegisterInstallmentPlanValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Error_Installment_Count_Below_Two(int count)
    {
        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.InstallmentCount = count;

        var result = new RegisterInstallmentPlanValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.INSTALLMENT_COUNT_INVALID);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-500)]
    public void Error_Total_Amount_Not_Greater_Than_Zero(decimal total)
    {
        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.TotalAmount = total;

        var result = new RegisterInstallmentPlanValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
