using Balance.Domain.Entities;
using Balance.Domain.Repositories.Accounts;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder rather than the usual static write-side builder: the ownership
/// assertions need to inspect the entity that was handed to the repository.
/// </summary>
public class AccountWriteOnlyRepositoryBuilder
{
    private readonly Mock<IAccountWriteOnlyRepository> _repository = new();

    public Account? Added { get; private set; }

    public AccountWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<Account>()))
            .Callback<Account>(account => Added = account);
    }

    public IAccountWriteOnlyRepository Build() => _repository.Object;
}
