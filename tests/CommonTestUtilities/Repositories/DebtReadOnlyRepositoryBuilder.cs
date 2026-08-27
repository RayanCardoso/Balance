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

    public DebtReadOnlyRepositoryBuilder GetAll(
        User user, Guid? creditorId, Guid? personId, bool includeInactive, List<Debt> debts)
    {
        _repository
            .Setup(repository => repository.GetAll(user, creditorId, personId, includeInactive))
            .ReturnsAsync(debts);

        return this;
    }

    public DebtReadOnlyRepositoryBuilder GetByCreditor(User user, Guid creditorId, List<Debt> debts)
    {
        _repository
            .Setup(repository => repository.GetByCreditor(user, creditorId))
            .ReturnsAsync(debts);

        return this;
    }

    public IDebtReadOnlyRepository Build() => _repository.Object;
}
