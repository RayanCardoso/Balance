using Balance.Domain.Entities;
using Balance.Domain.Repositories.Expenses;
using Moq;

namespace CommonTestUtilities.Repositories;

public class ExpenseReadOnlyRepositoryBuilder
{
    private readonly Mock<IExpenseReadOnlyRepository> _repository = new();

    public ExpenseReadOnlyRepositoryBuilder GetForMonth(
        User user,
        DateOnly competenceMonth,
        List<Expense> expenses)
    {
        _repository
            .Setup(repository => repository.GetForMonth(user, competenceMonth))
            .ReturnsAsync(expenses);

        return this;
    }

    public IExpenseReadOnlyRepository Build() => _repository.Object;
}
