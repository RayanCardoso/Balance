using Balance.Domain.Entities;
using Balance.Domain.Repositories.Users;
using Moq;

namespace CommonTestUtilities.Repositories;

public class UserReadOnlyRepositoryBuilder
{
    private readonly Mock<IUserReadOnlyRepository> _repository = new();

    public UserReadOnlyRepositoryBuilder ExistActiveUserWithEmail(string email)
    {
        _repository.Setup(r => r.ExistActiveUserWithEmail(email)).ReturnsAsync(true);

        return this;
    }

    public UserReadOnlyRepositoryBuilder GetByEmail(User user)
    {
        _repository.Setup(r => r.GetByEmail(user.Email)).ReturnsAsync(user);

        return this;
    }

    public IUserReadOnlyRepository Build() => _repository.Object;
}
