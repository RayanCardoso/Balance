using Bogus;
using Balance.Domain.Entities;
using Balance.Domain.Enums;

namespace CommonTestUtilities.Entities;

public class CategoryBuilder
{
    public static Category Build(User user)
    {
        return new Faker<Category>()
            .RuleFor(c => c.Id, _ => Guid.NewGuid())
            .RuleFor(c => c.Name, faker => faker.Commerce.Categories(1)[0])
            .RuleFor(c => c.Description, faker => faker.Lorem.Sentence())
            .RuleFor(c => c.Priority, faker => faker.PickRandom<ExpensePriority>())
            .RuleFor(c => c.UserId, _ => user.Id)
            .RuleFor(c => c.User, _ => user);
    }
}
