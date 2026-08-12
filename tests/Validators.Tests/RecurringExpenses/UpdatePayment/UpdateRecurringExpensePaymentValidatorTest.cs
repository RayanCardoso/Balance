using Balance.Application.UseCases.RecurringExpenses.UpdatePayment;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.RecurringExpenses.UpdatePayment;

public class UpdateRecurringExpensePaymentValidatorTest
{
    [Fact]
    public void Success()
    {
        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build();

        var result = new UpdateRecurringExpensePaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Null_Notes_And_Null_Paying_Account()
    {
        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build();
        request.Notes = null;
        request.AccountId = null;

        var result = new UpdateRecurringExpensePaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Error_Amount_Not_Greater_Than_Zero(decimal amountPaid)
    {
        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: amountPaid);

        var result = new UpdateRecurringExpensePaymentValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }
}
