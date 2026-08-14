using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterExpenseJsonBuilder
{
    public static RequestRegisterExpenseJson Build(Guid personId, Guid categoryId, Guid accountId)
    {
        return new Faker<RequestRegisterExpenseJson>()
            .RuleFor(r => r.Name, faker => faker.Commerce.ProductName())
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.Type, _ => ExpenseType.Credit)
            .RuleFor(r => r.Amount, faker => faker.Random.Decimal(10, 900))
            .RuleFor(r => r.CategoryId, _ => categoryId)
            .RuleFor(r => r.AccountId, _ => accountId)
            .RuleFor(r => r.Date, faker => DateOnly.FromDateTime(faker.Date.Recent(20)))
            .RuleFor(r => r.CompetenceMonth, _ => null);
    }
}
