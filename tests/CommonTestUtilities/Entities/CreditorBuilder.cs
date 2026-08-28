using Bogus;
using Balance.Domain.Entities;
using Balance.Domain.Enums;

namespace CommonTestUtilities.Entities;

public class CreditorBuilder
{
    public static Creditor Build(User user, bool archived = false)
    {
        return new Faker<Creditor>()
            .RuleFor(c => c.Id, _ => Guid.NewGuid())
            .RuleFor(c => c.Name, faker => faker.Person.FullName)
            .RuleFor(c => c.Type, faker => faker.PickRandom<CreditorType>())
            .RuleFor(c => c.Contact, faker => faker.Phone.PhoneNumber())
            .RuleFor(c => c.Notes, faker => faker.Lorem.Sentence())
            .RuleFor(c => c.Archived, _ => archived)
            .RuleFor(c => c.UserId, _ => user.Id)
            .RuleFor(c => c.User, _ => user);
    }
}
