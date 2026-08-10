using Balance.Domain.Entities;
using Balance.Domain.Repositories.Incomes;
using Moq;

namespace CommonTestUtilities.Repositories;

public class IncomeSourceReadOnlyRepositoryBuilder
{
    private readonly Mock<IIncomeSourceReadOnlyRepository> _repository = new();

    public IncomeSourceReadOnlyRepositoryBuilder GetById(User user, IncomeSource incomeSource)
    {
        _repository
            .Setup(repository => repository.GetById(user, incomeSource.Id))
            .ReturnsAsync(incomeSource);

        return this;
    }

    public IncomeSourceReadOnlyRepositoryBuilder GetForMonth(
        User user, DateOnly referenceMonth, List<IncomeSource> sources)
    {
        _repository
            .Setup(repository => repository.GetForMonth(user, referenceMonth))
            .ReturnsAsync(sources);

        return this;
    }

    public IIncomeSourceReadOnlyRepository Build() => _repository.Object;
}
