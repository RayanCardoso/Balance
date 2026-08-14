using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterRecurringExpenseJsonBuilder
{
    public static RequestRegisterRecurringExpenseJson Build(Guid personId, Guid categoryId, Guid accountId)
    {
        return new Faker<RequestRegisterRecurringExpenseJson>()
            .RuleFor(r => r.Name, faker => faker.Commerce.ProductName())
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.CategoryId, _ => categoryId)
            .RuleFor(r => r.AccountId, _ => accountId)
            .RuleFor(r => r.DueDay, faker => faker.Random.Int(1, 28))
            .RuleFor(r => r.Amount, faker => faker.Random.Decimal(50, 500))
            .RuleFor(r => r.IsEstimate, _ => false)
            .RuleFor(r => r.ValidityStart, _ => new DateOnly(2026, 1, 1))
            .RuleFor(r => r.ChangeReason, _ => "initial");
    }
}
