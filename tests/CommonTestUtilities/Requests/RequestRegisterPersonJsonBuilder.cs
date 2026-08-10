using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterPersonJsonBuilder
{
    public static RequestRegisterPersonJson Build()
    {
        return new Faker<RequestRegisterPersonJson>()
            .RuleFor(r => r.Name, faker => faker.Person.FirstName)
            .RuleFor(r => r.Description, faker => faker.Lorem.Sentence());
    }
}
