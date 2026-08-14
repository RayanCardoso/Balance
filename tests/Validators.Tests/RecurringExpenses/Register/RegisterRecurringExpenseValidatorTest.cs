using Balance.Application.UseCases.RecurringExpenses.Register;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.RecurringExpenses.Register;

public class RegisterRecurringExpenseValidatorTest
{
    [Fact]
    public void Success()
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = new RegisterRecurringExpenseValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty()
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Name = string.Empty;

        var result = new RegisterRecurringExpenseValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.NAME_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Error_Amount_Not_Greater_Than_Zero(decimal amount)
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Amount = amount;

        var result = new RegisterRecurringExpenseValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void Error_Due_Day_Out_Of_Range(int dueDay)
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.DueDay = dueDay;

        var result = new RegisterRecurringExpenseValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Success_Due_Day_Boundaries(int dueDay)
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.DueDay = dueDay;

        var result = new RegisterRecurringExpenseValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
