using Balance.Domain.Repositories;
using Moq;

namespace CommonTestUtilities.Repositories;

public class UnitOfWorkBuilder
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>How many times Commit() was called, for the single-transaction assertions.</summary>
    public int Commits { get; private set; }

    public UnitOfWorkBuilder()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Commit()).Callback(() => Commits++);
    }

    public static IUnitOfWork Build() => new Mock<IUnitOfWork>().Object;

    /// <summary>An instance whose Commit() calls are counted by <see cref="Commits"/>.</summary>
    public IUnitOfWork BuildCounting() => _unitOfWork.Object;
}
