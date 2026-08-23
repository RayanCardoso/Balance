using Balance.Application.UseCases.Expenses.Register;
using Balance.Communication.Enums;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Expenses.Register;

public class RegisterExpenseValidatorTest
{
    [Fact]
    public void Success()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = new RegisterExpenseValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Name = string.Empty;

        var result = new RegisterExpenseValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.NAME_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Error_Amount_Not_Greater_Than_Zero(decimal amount)
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Amount = amount;

        var result = new RegisterExpenseValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    [Fact]
    public void Error_Credit_Without_Account()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Type = ExpenseType.Credit;
        request.AccountId = null;

        var result = new RegisterExpenseValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.ACCOUNT_REQUIRED_FOR_CREDIT);
    }

    [Fact]
    public void Success_Pix_Without_Account()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Type = ExpenseType.Pix;
        request.AccountId = null;

        var result = new RegisterExpenseValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Debit_Without_Account()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Type = ExpenseType.Debit;
        request.AccountId = null;

        var result = new RegisterExpenseValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
