using Balance.Domain.Repositories;
using Moq;

namespace CommonTestUtilities.Repositories;

public class UnitOfWorkBuilder
{
    public static IUnitOfWork Build() => new Mock<IUnitOfWork>().Object;
}
