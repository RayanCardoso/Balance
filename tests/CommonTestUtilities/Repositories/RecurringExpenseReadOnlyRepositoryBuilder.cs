using Balance.Domain.Entities;
using Balance.Domain.Repositories.RecurringExpenses;
using Moq;

namespace CommonTestUtilities.Repositories;

public class RecurringExpenseReadOnlyRepositoryBuilder
{
    private readonly Mock<IRecurringExpenseReadOnlyRepository> _repository = new();

    public RecurringExpenseReadOnlyRepositoryBuilder GetById(User user, RecurringExpense recurringExpense)
    {
        _repository
            .Setup(repository => repository.GetById(user, recurringExpense.Id))
            .ReturnsAsync(recurringExpense);

        return this;
    }

    public IRecurringExpenseReadOnlyRepository Build() => _repository.Object;
}
