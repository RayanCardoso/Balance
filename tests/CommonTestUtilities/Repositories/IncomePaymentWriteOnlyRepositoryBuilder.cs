using Balance.Domain.Entities;
using Balance.Domain.Repositories.Incomes;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder rather than the usual static write-side builder: the tests assert on
/// the payment handed to the repository, including the frozen version reference.
/// </summary>
public class IncomePaymentWriteOnlyRepositoryBuilder
{
    private readonly Mock<IIncomePaymentWriteOnlyRepository> _repository = new();

    public IncomePayment? Added { get; private set; }

    public IncomePaymentWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<IncomePayment>()))
            .Callback<IncomePayment>(payment => Added = payment);
    }

    public IIncomePaymentWriteOnlyRepository Build() => _repository.Object;
}
