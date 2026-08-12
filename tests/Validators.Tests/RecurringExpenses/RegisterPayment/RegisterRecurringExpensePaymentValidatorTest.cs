using Balance.Application.UseCases.RecurringExpenses.RegisterPayment;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.RecurringExpenses.RegisterPayment;

public class RegisterRecurringExpensePaymentValidatorTest
{
    [Fact]
    public void Success()
    {
        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(Guid.NewGuid());

        var result = new RegisterRecurringExpensePaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Null_Notes_And_Null_Paying_Account()
    {
        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(Guid.NewGuid());
        request.Notes = null;
        request.AccountId = null;

        var result = new RegisterRecurringExpensePaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Error_Amount_Not_Greater_Than_Zero(decimal amountPaid)
    {
        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(Guid.NewGuid());
        request.AmountPaid = amountPaid;

        var result = new RegisterRecurringExpensePaymentValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
