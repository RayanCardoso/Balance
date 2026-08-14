using Balance.Domain.Entities;
using Balance.Domain.Repositories.Expenses;
using Moq;

namespace CommonTestUtilities.Repositories;

/// <summary>
/// Instance builder: the plan assertions need to inspect the entity that was handed
/// to the repository.
/// </summary>
public class InstallmentPlanWriteOnlyRepositoryBuilder
{
    private readonly Mock<IInstallmentPlanWriteOnlyRepository> _repository = new();

    public InstallmentPlan? Added { get; private set; }

    public InstallmentPlanWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.Add(It.IsAny<InstallmentPlan>()))
            .Callback<InstallmentPlan>(plan => Added = plan);
    }

    public IInstallmentPlanWriteOnlyRepository Build() => _repository.Object;
}
