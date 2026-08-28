using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the schedule assertions need to inspect the entity that was handed
/// to the repository.
/// </summary>
public class DebtWriteOnlyRepositoryBuilder
{
    private readonly Mock<IDebtWriteOnlyRepository> _repository = new();

    public Debt? Added { get; private set; }

    public DebtWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<Debt>()))
            .Callback<Debt>(debt => Added = debt);
    }

    public IDebtWriteOnlyRepository Build() => _repository.Object;
}
