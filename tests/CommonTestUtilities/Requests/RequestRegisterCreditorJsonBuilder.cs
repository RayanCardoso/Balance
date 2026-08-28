using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterCreditorJsonBuilder
{
    public static RequestRegisterCreditorJson Build()
    {
        return new Faker<RequestRegisterCreditorJson>()
            .RuleFor(r => r.Name, faker => faker.Person.FullName)
            .RuleFor(r => r.Type, faker => faker.PickRandom<CreditorType>())
            .RuleFor(r => r.Contact, faker => faker.Phone.PhoneNumber())
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence());
    }
}
