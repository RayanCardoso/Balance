using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestUpdateRecurringExpensePaymentJsonBuilder
{
    public static RequestUpdateRecurringExpensePaymentJson Build(
        decimal? amountPaid = null,
        DateOnly? paymentDate = null,
        Guid? accountId = null)
    {
        return new Faker<RequestUpdateRecurringExpensePaymentJson>()
            .RuleFor(r => r.AmountPaid, faker => amountPaid ?? faker.Random.Decimal(50, 500))
            .RuleFor(r => r.PaymentDate, _ => paymentDate ?? new DateOnly(2026, 8, 15))
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence())
            .RuleFor(r => r.AccountId, _ => accountId);
    }
}
