using Balance.Domain.Repositories.Users;
using Moq;

namespace CommonTestUtilities.Repositories;

public class UserWriteOnlyRepositoryBuilder
{
    public static IUserWriteOnlyRepository Build() => new Mock<IUserWriteOnlyRepository>().Object;
}
