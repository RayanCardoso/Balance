using Balance.Domain.Security.Cryptography;
using Moq;

namespace CommonTestUtilities.Cryptography;

public class PasswordEncripterBuilder
{
    private readonly Mock<IPasswordEncripter> _repository;

    public PasswordEncripterBuilder()
    {
        _repository = new Mock<IPasswordEncripter>();
        _repository.Setup(pe => pe.Encrypt(It.IsAny<string>())).Returns("hashed-password");
    }

    public PasswordEncripterBuilder Verify(string password)
    {
        _repository.Setup(pe => pe.Verify(password, It.IsAny<string>())).Returns(true);

        return this;
    }

    public IPasswordEncripter Build() => _repository.Object;
}
