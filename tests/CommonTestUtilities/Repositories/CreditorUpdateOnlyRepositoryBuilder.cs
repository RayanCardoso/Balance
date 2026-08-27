using Balance.Domain.Entities;
using Balance.Domain.Repositories.Creditors;
using Moq;

namespace CommonTestUtilities.Repositories;

public class CreditorUpdateOnlyRepositoryBuilder
{
    private readonly Mock<ICreditorUpdateOnlyRepository> _repository = new();

    public CreditorUpdateOnlyRepositoryBuilder GetById(User user, Creditor creditor)
    {
        _repository
            .Setup(repository => repository.GetById(user, creditor.Id))
            .ReturnsAsync(creditor);

        return this;
    }

    public ICreditorUpdateOnlyRepository Build() => _repository.Object;
}
