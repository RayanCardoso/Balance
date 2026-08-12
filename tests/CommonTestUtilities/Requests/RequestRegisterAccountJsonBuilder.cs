using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterAccountJsonBuilder
{
    public static RequestRegisterAccountJson Build(Guid personId)
    {
        return new Faker<RequestRegisterAccountJson>()
            .RuleFor(r => r.Name, faker => faker.Finance.AccountName())
            .RuleFor(r => r.Institution, faker => faker.Company.CompanyName())
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.ClosingDay, faker => faker.Random.Int(1, 28))
            .RuleFor(r => r.DueDay, faker => faker.Random.Int(1, 28))
            .RuleFor(r => r.Limit, faker => faker.Random.Decimal(1000, 9000));
    }

    /// <summary>A debit account: no closing day, due day or limit.</summary>
    public static RequestRegisterAccountJson Debit(Guid personId)
    {
        return new Faker<RequestRegisterAccountJson>()
            .RuleFor(r => r.Name, faker => faker.Finance.AccountName())
            .RuleFor(r => r.Institution, faker => faker.Company.CompanyName())
            .RuleFor(r => r.PersonId, _ => personId)
            .RuleFor(r => r.ClosingDay, _ => null)
            .RuleFor(r => r.DueDay, _ => null)
            .RuleFor(r => r.Limit, _ => null);
    }
}
