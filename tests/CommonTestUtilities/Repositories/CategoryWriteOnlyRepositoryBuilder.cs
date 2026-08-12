using Balance.Domain.Entities;
using Balance.Domain.Repositories.Categories;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder rather than the usual static write-side builder: the ownership
/// assertions need to inspect the entity that was handed to the repository.
/// </summary>
public class CategoryWriteOnlyRepositoryBuilder
{
    private readonly Mock<ICategoryWriteOnlyRepository> _repository = new();

    public Category? Added { get; private set; }

    public CategoryWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<Category>()))
            .Callback<Category>(category => Added = category);
    }

    public ICategoryWriteOnlyRepository Build() => _repository.Object;
}
