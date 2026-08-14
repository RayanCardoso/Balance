using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterCategoryJsonBuilder
{
    public static RequestRegisterCategoryJson Build()
    {
        return new Faker<RequestRegisterCategoryJson>()
            .RuleFor(r => r.Name, faker => faker.Commerce.Categories(1)[0])
            .RuleFor(r => r.Description, faker => faker.Lorem.Sentence())
            .RuleFor(r => r.Priority, faker => faker.PickRandom<ExpensePriority>());
    }
}
