using Balance.Domain.Entities;
using Balance.Domain.Repositories.Expenses;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the competence-month and installment assertions need to inspect the
/// entities that were handed to the repository.
/// </summary>
public class ExpenseWriteOnlyRepositoryBuilder
{
    private readonly Mock<IExpenseWriteOnlyRepository> _repository = new();

    public Expense? Added { get; private set; }

    public List<Expense> AddedRange { get; private set; } = [];

    public ExpenseWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<Expense>()))
            .Callback<Expense>(expense => Added = expense);

        _repository
            .Setup(repository => repository.AddRange(It.IsAny<IEnumerable<Expense>>()))
            .Callback<IEnumerable<Expense>>(expenses => AddedRange = expenses.ToList());
    }

    public IExpenseWriteOnlyRepository Build() => _repository.Object;
}
