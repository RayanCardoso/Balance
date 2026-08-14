using Balance.Application.UseCases.RecurringExpenses.ChangeValue;
using Balance.Exception;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.RecurringExpenses.ChangeValue;

public class ChangeRecurringExpenseValueValidatorTest
{
    [Fact]
    public void Success()
    {
        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(Guid.NewGuid());

        var result = new ChangeRecurringExpenseValueValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Change_Reason_Empty()
    {
        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(Guid.NewGuid());
        request.ChangeReason = string.Empty;

        var result = new ChangeRecurringExpenseValueValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == ResourceErrorMessages.CHANGE_REASON_REQUIRED);
    }
}
