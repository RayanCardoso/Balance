using Balance.Domain.Entities;
using Balance.Domain.Repositories.Incomes;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder rather than the usual static write-side builder: the tests assert
/// on the entities handed to the repository, which a static builder cannot expose.
/// </summary>
public class IncomeSourceWriteOnlyRepositoryBuilder
{
    private readonly Mock<IIncomeSourceWriteOnlyRepository> _repository = new();

    public IncomeSource? AddedSource { get; private set; }
    public List<IncomeSourceVersion> AddedVersions { get; } = [];

    public IncomeSourceWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<IncomeSource>()))
            .Callback<IncomeSource>(source => AddedSource = source);

        _repository
            .Setup(repository => repository.AddVersion(It.IsAny<IncomeSourceVersion>()))
            .Callback<IncomeSourceVersion>(AddedVersions.Add);
    }

    public IIncomeSourceWriteOnlyRepository Build() => _repository.Object;
}
