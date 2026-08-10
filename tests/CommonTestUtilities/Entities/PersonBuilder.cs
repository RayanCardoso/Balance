using Bogus;
using Balance.Domain.Entities;
using Person = Balance.Domain.Entities.Person;

namespace CommonTestUtilities.Entities;

public class PersonBuilder
{
    public static Person Build(User user, bool isAccountOwner = false)
    {
        return new Faker<Person>()
            .RuleFor(p => p.Id, _ => Guid.NewGuid())
            .RuleFor(p => p.Name, faker => faker.Person.FirstName)
            .RuleFor(p => p.Description, faker => faker.Lorem.Sentence())
            .RuleFor(p => p.IsAccountOwner, _ => isAccountOwner)
            .RuleFor(p => p.UserId, _ => user.Id)
            .RuleFor(p => p.User, _ => user);
    }
}
