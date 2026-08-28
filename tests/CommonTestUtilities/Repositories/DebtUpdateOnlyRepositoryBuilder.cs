using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Moq;

namespace CommonTestUtilities.Repositories;

public class DebtUpdateOnlyRepositoryBuilder
{
    private readonly Mock<IDebtUpdateOnlyRepository> _repository = new();

    public DebtUpdateOnlyRepositoryBuilder GetById(User user, Debt debt)
    {
        _repository.Setup(repository => repository.GetById(user, debt.Id)).ReturnsAsync(debt);

        return this;
    }

    public IDebtUpdateOnlyRepository Build() => _repository.Object;
}
