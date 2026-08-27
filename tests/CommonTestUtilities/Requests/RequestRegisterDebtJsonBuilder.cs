using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterDebtJsonBuilder
{
    public static RequestRegisterDebtJson BuildScheduled()
    {
        return Base()
            .RuleFor(r => r.Mode, _ => DebtMode.Scheduled)
            .RuleFor(r => r.InstallmentCount, faker => faker.Random.Int(1, 12))
            .RuleFor(r => r.DueDay, faker => faker.Random.Int(1, 31));
    }

    public static RequestRegisterDebtJson BuildOpenEnded()
    {
        return Base()
            .RuleFor(r => r.Mode, _ => DebtMode.OpenEnded)
            .RuleFor(r => r.InstallmentCount, _ => null)
            .RuleFor(r => r.DueDay, _ => null);
    }

    private static Faker<RequestRegisterDebtJson> Base()
    {
        return new Faker<RequestRegisterDebtJson>()
            .RuleFor(r => r.Name, faker => faker.Person.FullName)
            .RuleFor(r => r.CreditorId, _ => Guid.NewGuid())
            .RuleFor(r => r.PersonId, _ => Guid.NewGuid())
            .RuleFor(r => r.CategoryId, _ => Guid.NewGuid())
            .RuleFor(r => r.PrincipalAmount, faker => faker.Random.Decimal(100, 900))
            .RuleFor(r => r.TotalAmount, (_, r) => r.PrincipalAmount)
            .RuleFor(r => r.StartDate, faker => DateOnly.FromDateTime(faker.Date.Recent(20)))
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence());
    }
}
