using Bogus;
using Balance.Domain.Entities;
using Balance.Domain.Enums;

namespace CommonTestUtilities.Entities;

public class UserBuilder
{
    public static User Build(string role = Roles.TEAM_MEMBER)
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, _ => 1)
            .RuleFor(u => u.Name, faker => faker.Person.FirstName)
            .RuleFor(u => u.Email, (faker, user) => faker.Internet.Email(user.Name))
            .RuleFor(u => u.Password, faker => faker.Internet.Password(length: 8))
            .RuleFor(u => u.UserIdentifier, _ => Guid.NewGuid())
            .RuleFor(u => u.Role, _ => role);
    }
}
