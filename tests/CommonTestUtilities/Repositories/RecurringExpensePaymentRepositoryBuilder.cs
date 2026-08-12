using Balance.Domain.Entities;
using Balance.Domain.Repositories.RecurringExpenses;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the tests assert on the payment handed to the repository, including the
/// frozen version reference.
/// </summary>
public class RecurringExpensePaymentRepositoryBuilder
{
    private readonly Mock<IRecurringExpensePaymentRepository> _repository = new();

    public RecurringExpensePayment? Added { get; private set; }

    public RecurringExpensePaymentRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<RecurringExpensePayment>()))
            .Callback<RecurringExpensePayment>(payment => Added = payment);
    }

    /// <summary>Makes the duplicate probe answer with an already recorded payment for that month.</summary>
    public RecurringExpensePaymentRepositoryBuilder GetByMonth(
        Guid recurringExpenseId, DateOnly referenceMonth, RecurringExpensePayment payment)
    {
        _repository
            .Setup(repository => repository.GetByMonth(recurringExpenseId, referenceMonth))
            .ReturnsAsync(payment);

        return this;
    }

    /// <summary>The tracked read the correction path loads the payment through.</summary>
    public RecurringExpensePaymentRepositoryBuilder GetById(User user, RecurringExpensePayment payment)
    {
        _repository
            .Setup(repository => repository.GetById(user, payment.Id))
            .ReturnsAsync(payment);

        return this;
    }

    public IRecurringExpensePaymentRepository Build() => _repository.Object;
}
