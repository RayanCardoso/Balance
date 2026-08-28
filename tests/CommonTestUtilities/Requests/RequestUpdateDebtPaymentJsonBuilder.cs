using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestUpdateDebtPaymentJsonBuilder
{
    public static RequestUpdateDebtPaymentJson Build(
        decimal? amountPaid = null,
        DateOnly? paymentDate = null,
        Guid? accountId = null,
        ExpenseType? type = null)
    {
        return new Faker<RequestUpdateDebtPaymentJson>()
            .RuleFor(r => r.AmountPaid, faker => amountPaid ?? faker.Random.Decimal(50, 500))
            .RuleFor(r => r.PaymentDate, _ => paymentDate ?? new DateOnly(2026, 8, 15))
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence())
            .RuleFor(r => r.AccountId, _ => accountId)
            .RuleFor(r => r.Type, _ => type);
    }
}
