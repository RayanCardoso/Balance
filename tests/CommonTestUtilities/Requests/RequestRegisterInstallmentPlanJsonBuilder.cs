using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterInstallmentPlanJsonBuilder
{
    public static RequestRegisterInstallmentPlanJson Build(Guid personId, Guid categoryId, Guid accountId)
    {
        return new Faker<RequestRegisterInstallmentPlanJson>()
            .RuleFor(r => r.Name, faker => faker.Commerce.ProductName())
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.TotalAmount, faker => faker.Random.Decimal(100, 900))
            .RuleFor(r => r.InstallmentCount, faker => faker.Random.Int(2, 12))
            .RuleFor(r => r.CategoryId, _ => categoryId)
            .RuleFor(r => r.AccountId, _ => accountId)
            .RuleFor(r => r.StartDate, faker => DateOnly.FromDateTime(faker.Date.Recent(20)));
    }
}
