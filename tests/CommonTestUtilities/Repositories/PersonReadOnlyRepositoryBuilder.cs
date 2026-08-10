using Balance.Domain.Entities;
using Balance.Domain.Repositories.People;
using Moq;

namespace CommonTestUtilities.Repositories;

public class PersonReadOnlyRepositoryBuilder
{
    private readonly Mock<IPersonReadOnlyRepository> _repository = new();

    public PersonReadOnlyRepositoryBuilder GetAll(User user, List<Person> people)
    {
        _repository.Setup(repository => repository.GetAll(user)).ReturnsAsync(people);

        return this;
    }

    public PersonReadOnlyRepositoryBuilder GetById(User user, Person person)
    {
        _repository.Setup(repository => repository.GetById(user, person.Id)).ReturnsAsync(person);

        return this;
    }

    public IPersonReadOnlyRepository Build() => _repository.Object;
}
