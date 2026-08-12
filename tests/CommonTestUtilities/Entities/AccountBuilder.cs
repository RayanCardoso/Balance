using Bogus;
using Balance.Domain.Entities;
using Person = Balance.Domain.Entities.Person;

namespace CommonTestUtilities.Entities;

public class AccountBuilder
{
    public static Account Build(Person person)
    {
        return new Faker<Account>()
            .RuleFor(a => a.Id, _ => Guid.NewGuid())
            .RuleFor(a => a.Name, faker => faker.Finance.AccountName())
            .RuleFor(a => a.Institution, faker => faker.Company.CompanyName())
            .RuleFor(a => a.ClosingDay, faker => faker.Random.Int(1, 28))
            .RuleFor(a => a.DueDay, faker => faker.Random.Int(1, 28))
            .RuleFor(a => a.Limit, faker => faker.Random.Decimal(1000, 9000))
            .RuleFor(a => a.PersonId, _ => person.Id)
            .RuleFor(a => a.Person, _ => person);
    }
}
