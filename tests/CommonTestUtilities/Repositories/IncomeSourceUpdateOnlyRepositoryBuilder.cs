using Balance.Domain.Entities;
using Balance.Domain.Repositories.Incomes;
using Moq;

namespace CommonTestUtilities.Repositories;

public class IncomeSourceUpdateOnlyRepositoryBuilder
{
    private readonly Mock<IIncomeSourceUpdateOnlyRepository> _repository = new();

    public IncomeSourceUpdateOnlyRepositoryBuilder GetById(User user, IncomeSource incomeSource)
    {
        _repository
            .Setup(repository => repository.GetById(user, incomeSource.Id))
            .ReturnsAsync(incomeSource);

        return this;
    }

    public IIncomeSourceUpdateOnlyRepository Build() => _repository.Object;
}
