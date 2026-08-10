using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterIncomeSourceJsonBuilder
{
    public static RequestRegisterIncomeSourceJson Recurring(Guid personId)
    {
        return new Faker<RequestRegisterIncomeSourceJson>()
            .RuleFor(r => r.Name, faker => faker.Commerce.Department())
            .RuleFor(r => r.Type, _ => IncomeType.Recurring)
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.Amount, faker => faker.Random.Decimal(1000, 9000))
            .RuleFor(r => r.ExpectedDay, faker => faker.Random.Int(1, 28))
            .RuleFor(r => r.ValidityStart, _ => new DateOnly(2026, 1, 1))
            .RuleFor(r => r.ChangeReason, _ => "initial");
    }

    public static RequestRegisterIncomeSourceJson Variable(Guid personId)
    {
        return new Faker<RequestRegisterIncomeSourceJson>()
            .RuleFor(r => r.Name, faker => faker.Commerce.Department())
            .RuleFor(r => r.Type, _ => IncomeType.Variable)
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.Amount, _ => null)
            .RuleFor(r => r.ExpectedDay, _ => null)
            .RuleFor(r => r.ValidityStart, _ => null)
            .RuleFor(r => r.ChangeReason, _ => null);
    }
}
