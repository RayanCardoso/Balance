using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the schedule assertions need to inspect the installments that were
/// handed to the repository.
/// </summary>
public class DebtInstallmentWriteOnlyRepositoryBuilder
{
    private readonly Mock<IDebtInstallmentWriteOnlyRepository> _repository = new();

    public List<DebtInstallment> AddedRange { get; private set; } = [];

    public DebtInstallmentWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.AddRange(It.IsAny<IEnumerable<DebtInstallment>>()))
            .Callback<IEnumerable<DebtInstallment>>(installments => AddedRange = installments.ToList());
    }

    public IDebtInstallmentWriteOnlyRepository Build() => _repository.Object;
}
