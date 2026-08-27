using Bogus;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Person = Balance.Domain.Entities.Person;

namespace CommonTestUtilities.Entities;

public class DebtBuilder
{
    public static Debt Build(
        Person person,
        Creditor creditor,
        Category category,
        DebtMode mode = DebtMode.Scheduled,
        bool archived = false)
    {
        return new Faker<Debt>()
            .RuleFor(d => d.Id, _ => Guid.NewGuid())
            .RuleFor(d => d.Name, faker => faker.Lorem.Sentence(3))
            .RuleFor(d => d.Mode, _ => mode)
            .RuleFor(d => d.PrincipalAmount, faker => faker.Random.Decimal(100, 900))
            .RuleFor(d => d.TotalAmount, (_, debt) => debt.PrincipalAmount)
            .RuleFor(d => d.StartDate, faker => DateOnly.FromDateTime(faker.Date.Recent(20)))
            .RuleFor(d => d.DueDay, faker => mode == DebtMode.Scheduled ? faker.Random.Int(1, 28) : null)
            .RuleFor(d => d.InstallmentCount, faker => mode == DebtMode.Scheduled ? faker.Random.Int(1, 12) : null)
            .RuleFor(d => d.EndMonth, (_, debt) => mode == DebtMode.Scheduled ? debt.StartDate : null)
            .RuleFor(d => d.Archived, _ => archived)
            .RuleFor(d => d.Notes, faker => faker.Lorem.Sentence())
            .RuleFor(d => d.CreditorId, _ => creditor.Id)
            .RuleFor(d => d.Creditor, _ => creditor)
            .RuleFor(d => d.PersonId, _ => person.Id)
            .RuleFor(d => d.Person, _ => person)
            .RuleFor(d => d.CategoryId, _ => category.Id)
            .RuleFor(d => d.Category, _ => category);
    }
}
