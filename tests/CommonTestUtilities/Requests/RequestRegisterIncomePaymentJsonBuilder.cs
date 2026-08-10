using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterIncomePaymentJsonBuilder
{
    public static RequestRegisterIncomePaymentJson Build(
        Guid incomeSourceId,
        DateOnly? referenceMonth = null,
        DateOnly? paymentDate = null)
    {
        var month = referenceMonth ?? new DateOnly(2026, 3, 1);

        return new Faker<RequestRegisterIncomePaymentJson>()
            .RuleFor(r => r.IncomeSourceId, _ => incomeSourceId)
            .RuleFor(r => r.ReferenceMonth, _ => month)
            .RuleFor(r => r.PaymentDate, _ => paymentDate ?? month.AddDays(4))
            .RuleFor(r => r.AmountReceived, faker => faker.Random.Decimal(1000, 9000))
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence());
    }
}
