using Balance.Domain.Entities;
using Balance.Domain.Repositories.Creditors;
using Moq;

namespace CommonTestUtilities.Repositories;

public class CreditorReadOnlyRepositoryBuilder
{
    private readonly Mock<ICreditorReadOnlyRepository> _repository = new();

    public CreditorReadOnlyRepositoryBuilder GetAll(User user, bool includeArchived, List<Creditor> creditors)
    {
        _repository.Setup(repository => repository.GetAll(user, includeArchived)).ReturnsAsync(creditors);

        return this;
    }

    public CreditorReadOnlyRepositoryBuilder GetById(User user, Creditor creditor)
    {
        _repository.Setup(repository => repository.GetById(user, creditor.Id)).ReturnsAsync(creditor);

        return this;
    }

    public ICreditorReadOnlyRepository Build() => _repository.Object;
}
