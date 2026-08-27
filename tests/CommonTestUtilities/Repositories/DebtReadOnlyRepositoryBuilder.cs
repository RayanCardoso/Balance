using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Moq;

namespace CommonTestUtilities.Repositories;

public class DebtReadOnlyRepositoryBuilder
{
    private readonly Mock<IDebtReadOnlyRepository> _repository = new();

    public DebtReadOnlyRepositoryBuilder GetById(User user, Debt debt)
    {
        _repository.Setup(repository => repository.GetById(user, debt.Id)).ReturnsAsync(debt);

        return this;
    }

    public IDebtReadOnlyRepository Build() => _repository.Object;
}
