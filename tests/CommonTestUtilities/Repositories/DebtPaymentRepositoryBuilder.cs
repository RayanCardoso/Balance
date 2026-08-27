using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the assertions need to inspect the payment handed to the repository, and
/// the duplicate-installment probe and the tracked correction read both need to be steerable per test.
/// </summary>
public class DebtPaymentRepositoryBuilder
{
    private readonly Mock<IDebtPaymentRepository> _repository = new();

    public DebtPayment? Added { get; private set; }

    public DebtPaymentRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<DebtPayment>()))
            .Callback<DebtPayment>(payment => Added = payment);
    }

    /// <summary>Makes the duplicate probe answer with an already recorded payment for that installment.</summary>
    public DebtPaymentRepositoryBuilder GetByInstallment(User user, Guid installmentId, DebtPayment payment)
    {
        _repository
            .Setup(repository => repository.GetByInstallment(user, installmentId))
            .ReturnsAsync(payment);

        return this;
    }

    /// <summary>The tracked read the correction path loads the payment through.</summary>
    public DebtPaymentRepositoryBuilder GetById(User user, DebtPayment payment)
    {
        _repository
            .Setup(repository => repository.GetById(user, payment.Id))
            .ReturnsAsync(payment);

        return this;
    }

    public IDebtPaymentRepository Build() => _repository.Object;
}
