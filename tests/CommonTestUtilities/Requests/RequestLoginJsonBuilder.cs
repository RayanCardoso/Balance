using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestLoginJsonBuilder
{
    public static RequestLoginJson Build()
    {
        return new Faker<RequestLoginJson>()
            .RuleFor(r => r.Email, faker => faker.Internet.Email())
            .RuleFor(r => r.Password, faker => faker.Internet.Password());
    }
}
