using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestChangeRecurringExpenseValueJsonBuilder
{
    public static RequestChangeRecurringExpenseValueJson Build(
        Guid recurringExpenseId,
        DateOnly? validityStart = null)
    {
        return new Faker<RequestChangeRecurringExpenseValueJson>()
            .RuleFor(r => r.RecurringExpenseId, _ => recurringExpenseId)
            .RuleFor(r => r.Amount, faker => faker.Random.Decimal(200, 400))
            .RuleFor(r => r.ValidityStart, _ => validityStart ?? new DateOnly(2026, 9, 1))
            .RuleFor(r => r.ChangeReason, _ => "tariff increase");
    }
}
