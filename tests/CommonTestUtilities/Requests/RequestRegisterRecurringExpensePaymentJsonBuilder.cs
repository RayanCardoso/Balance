using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterRecurringExpensePaymentJsonBuilder
{
    public static RequestRegisterRecurringExpensePaymentJson Build(
        Guid recurringExpenseId,
        DateOnly? referenceMonth = null,
        Guid? accountId = null)
    {
        var month = referenceMonth ?? new DateOnly(2026, 8, 1);

        return new Faker<RequestRegisterRecurringExpensePaymentJson>()
            .RuleFor(r => r.RecurringExpenseId, _ => recurringExpenseId)
            .RuleFor(r => r.ReferenceMonth, _ => month)
            .RuleFor(r => r.PaymentDate, _ => month.AddDays(9))
            .RuleFor(r => r.AmountPaid, faker => faker.Random.Decimal(50, 500))
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence())
            .RuleFor(r => r.AccountId, _ => accountId);
    }
}
