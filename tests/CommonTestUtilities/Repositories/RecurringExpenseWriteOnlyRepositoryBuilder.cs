using Balance.Domain.Entities;
using Balance.Domain.Repositories.RecurringExpenses;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the transaction assertions inspect the entities handed to the repository,
/// which a static builder cannot expose.
/// </summary>
public class RecurringExpenseWriteOnlyRepositoryBuilder
{
    private readonly Mock<IRecurringExpenseWriteOnlyRepository> _repository = new();

    public RecurringExpense? Added { get; private set; }
    public List<RecurringExpenseVersion> AddedVersions { get; } = [];

    public RecurringExpenseWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<RecurringExpense>()))
            .Callback<RecurringExpense>(expense => Added = expense);

        _repository
            .Setup(repository => repository.AddVersion(It.IsAny<RecurringExpenseVersion>()))
            .Callback<RecurringExpenseVersion>(AddedVersions.Add);
    }

    public IRecurringExpenseWriteOnlyRepository Build() => _repository.Object;
}
