using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestChangeIncomeSourceValueJsonBuilder
{
    public static RequestChangeIncomeSourceValueJson Build(Guid incomeSourceId, DateOnly? validityStart = null)
    {
        return new Faker<RequestChangeIncomeSourceValueJson>()
            .RuleFor(r => r.IncomeSourceId, _ => incomeSourceId)
            .RuleFor(r => r.Amount, faker => faker.Random.Decimal(6000, 12000))
            .RuleFor(r => r.ExpectedDay, faker => faker.Random.Int(1, 28))
            .RuleFor(r => r.ValidityStart, _ => validityStart ?? new DateOnly(2026, 7, 1))
            .RuleFor(r => r.ChangeReason, _ => "promotion");
    }
}
