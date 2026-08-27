using Balance.Domain.Entities;
using Balance.Domain.Repositories.Creditors;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder rather than the usual static write-side builder: the ownership
/// assertions need to inspect the entity that was handed to the repository.
/// </summary>
public class CreditorWriteOnlyRepositoryBuilder
{
    private readonly Mock<ICreditorWriteOnlyRepository> _repository = new();

    public Creditor? Added { get; private set; }

    public CreditorWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<Creditor>()))
            .Callback<Creditor>(creditor => Added = creditor);
    }

    public ICreditorWriteOnlyRepository Build() => _repository.Object;
}
