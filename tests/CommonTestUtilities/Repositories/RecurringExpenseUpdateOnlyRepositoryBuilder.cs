using Balance.Domain.Entities;
using Balance.Domain.Repositories.RecurringExpenses;
using Moq;

namespace CommonTestUtilities.Repositories;

public class RecurringExpenseUpdateOnlyRepositoryBuilder
{
    private readonly Mock<IRecurringExpenseUpdateOnlyRepository> _repository = new();

    public RecurringExpenseUpdateOnlyRepositoryBuilder GetById(User user, RecurringExpense recurringExpense)
    {
        _repository
            .Setup(repository => repository.GetById(user, recurringExpense.Id))
            .ReturnsAsync(recurringExpense);

        return this;
    }

    public IRecurringExpenseUpdateOnlyRepository Build() => _repository.Object;
}
