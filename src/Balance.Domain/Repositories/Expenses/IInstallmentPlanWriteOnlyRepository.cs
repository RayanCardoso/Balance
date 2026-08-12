using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Expenses;

public interface IInstallmentPlanWriteOnlyRepository
{
    Task Add(InstallmentPlan plan);
}
