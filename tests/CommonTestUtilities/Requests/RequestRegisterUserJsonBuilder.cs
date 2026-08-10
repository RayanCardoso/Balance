using Bogus;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterUserJsonBuilder
{
    public static RequestRegisterUserJson Build(int passwordLength = 10)
    {
        return new Faker<RequestRegisterUserJson>()
            .RuleFor(r => r.Name, faker => faker.Person.FirstName)
            .RuleFor(r => r.Email, (faker, request) => faker.Internet.Email(request.Name))
            .RuleFor(r => r.Password, faker => faker.Internet.Password(passwordLength));
    }
}
