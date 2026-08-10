using Balance.Domain.Entities;
using Balance.Domain.Repositories.People;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder rather than the usual static write-side builder: the ownership
/// assertions need to inspect the entity that was handed to the repository.
/// </summary>
public class PersonWriteOnlyRepositoryBuilder
{
    private readonly Mock<IPersonWriteOnlyRepository> _repository = new();

    public Person? Added { get; private set; }

    public PersonWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<Person>()))
            .Callback<Person>(person => Added = person);
    }

    public IPersonWriteOnlyRepository Build() => _repository.Object;
}
