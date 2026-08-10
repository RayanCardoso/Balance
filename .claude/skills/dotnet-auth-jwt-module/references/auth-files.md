# JWT authentication module files

`__PROJECT_NAME__` is replaced by the solution name throughout, in paths and contents.

---

## 1. Domain

### `src/__PROJECT_NAME__.Domain/Enums/Roles.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Enums;

public static class Roles
{
    public const string ADMIN = "Admin";
    public const string TEAM_MEMBER = "TeamMember";
}
```

### `src/__PROJECT_NAME__.Domain/Entities/User.cs`

> `UserIdentifier` is the public handle put in the token. The numeric `Id` never leaves the
> database, so leaking a token does not leak a row count.

```csharp
using __PROJECT_NAME__.Domain.Enums;

namespace __PROJECT_NAME__.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid UserIdentifier { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = Roles.TEAM_MEMBER;
}
```

### `src/__PROJECT_NAME__.Domain/Repositories/Users/IUserReadOnlyRepository.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Repositories.Users;

public interface IUserReadOnlyRepository
{
    Task<bool> ExistActiveUserWithEmail(string email);
    Task<Entities.User?> GetByEmail(string email);
}
```

### `src/__PROJECT_NAME__.Domain/Repositories/Users/IUserWriteOnlyRepository.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Repositories.Users;

public interface IUserWriteOnlyRepository
{
    Task Add(Entities.User user);
}
```

### `src/__PROJECT_NAME__.Domain/Security/Cryptography/IPasswordEncripter.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Security.Cryptography;

public interface IPasswordEncripter
{
    string Encrypt(string password);
    bool Verify(string password, string passwordHash);
}
```

### `src/__PROJECT_NAME__.Domain/Security/Tokens/IAccessTokenGenerator.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;

namespace __PROJECT_NAME__.Domain.Security.Tokens;

public interface IAccessTokenGenerator
{
    string Generate(User user);
}
```

### `src/__PROJECT_NAME__.Domain/Security/Tokens/ITokenProvider.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Security.Tokens;

public interface ITokenProvider
{
    string TokenOnRequest();
}
```

### `src/__PROJECT_NAME__.Domain/Services/LoggedUser/ILoggedUser.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;

namespace __PROJECT_NAME__.Domain.Services.LoggedUser;

public interface ILoggedUser
{
    Task<User> Get();
}
```

---

## 2. Exception layer

### `src/__PROJECT_NAME__.Exception/ExceptionBase/InvalidLoginException.cs`

```csharp
using System.Net;

namespace __PROJECT_NAME__.Exception.ExceptionBase;

public class InvalidLoginException : __PROJECT_NAME__Exception
{
    public InvalidLoginException() : base(ResourceErrorMessages.EMAIL_OR_PASSWORD_INVALID)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.Unauthorized;

    public override List<string> GetErrors() => [Message];
}
```

### Messages to add

Add each key to **both** `.resx` files, then add the matching property to
`ResourceErrorMessages.cs`:

```csharp
    public static string NAME_REQUIRED => Get(nameof(NAME_REQUIRED));
    public static string EMAIL_REQUIRED => Get(nameof(EMAIL_REQUIRED));
    public static string EMAIL_INVALID => Get(nameof(EMAIL_INVALID));
    public static string EMAIL_ALREADY_REGISTERED => Get(nameof(EMAIL_ALREADY_REGISTERED));
    public static string PASSWORD_REQUIRED => Get(nameof(PASSWORD_REQUIRED));
    public static string PASSWORD_TOO_SHORT => Get(nameof(PASSWORD_TOO_SHORT));
    public static string EMAIL_OR_PASSWORD_INVALID => Get(nameof(EMAIL_OR_PASSWORD_INVALID));
```

| Key | en | pt-BR |
| --- | --- | --- |
| `NAME_REQUIRED` | Name is required. | O nome é obrigatório. |
| `EMAIL_REQUIRED` | E-mail is required. | O e-mail é obrigatório. |
| `EMAIL_INVALID` | E-mail is invalid. | O e-mail é inválido. |
| `EMAIL_ALREADY_REGISTERED` | E-mail is already registered. | E-mail já cadastrado. |
| `PASSWORD_REQUIRED` | Password is required. | A senha é obrigatória. |
| `PASSWORD_TOO_SHORT` | Password must be at least 6 characters long. | A senha deve ter ao menos 6 caracteres. |
| `EMAIL_OR_PASSWORD_INVALID` | E-mail and/or password are invalid. | E-mail e/ou senha inválidos. |

---

## 3. Communication

### `src/__PROJECT_NAME__.Communication/Requests/RequestRegisterUserJson.cs`

```csharp
namespace __PROJECT_NAME__.Communication.Requests;

public class RequestRegisterUserJson
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### `src/__PROJECT_NAME__.Communication/Requests/RequestLoginJson.cs`

```csharp
namespace __PROJECT_NAME__.Communication.Requests;

public class RequestLoginJson
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### `src/__PROJECT_NAME__.Communication/Responses/ResponseRegisteredUserJson.cs`

```csharp
namespace __PROJECT_NAME__.Communication.Responses;

public class ResponseRegisteredUserJson
{
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
```

---

## 4. Infrastructure

### `src/__PROJECT_NAME__.Infrastructure/Security/Cryptography/PasswordEncripter.cs`

> Named `PasswordEncripter`, not `BCrypt` — a class called `BCrypt` collides with the
> `BCrypt.Net` namespace and forces fully-qualified names at every call site.

```csharp
using __PROJECT_NAME__.Domain.Security.Cryptography;

namespace __PROJECT_NAME__.Infrastructure.Security.Cryptography;

internal class PasswordEncripter : IPasswordEncripter
{
    public string Encrypt(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
```

### `src/__PROJECT_NAME__.Infrastructure/Security/Tokens/JwtTokenGenerator.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Security.Tokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace __PROJECT_NAME__.Infrastructure.Security.Tokens;

public class JwtTokenGenerator : IAccessTokenGenerator
{
    private readonly uint _expirationTimeMinutes;
    private readonly string _signingKey;

    public JwtTokenGenerator(uint expirationTimeMinutes, string signingKey)
    {
        _expirationTimeMinutes = expirationTimeMinutes;
        _signingKey = signingKey;
    }

    public string Generate(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Sid, user.UserIdentifier.ToString()),
            new(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddMinutes(_expirationTimeMinutes),
            SigningCredentials = new SigningCredentials(SecurityKey(), SecurityAlgorithms.HmacSha256Signature),
            Subject = new ClaimsIdentity(claims)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }

    private SymmetricSecurityKey SecurityKey() =>
        new(Encoding.UTF8.GetBytes(_signingKey));
}
```

### `src/__PROJECT_NAME__.Infrastructure/Services/LoggedUser/LoggedUser.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Security.Tokens;
using __PROJECT_NAME__.Domain.Services.LoggedUser;
using __PROJECT_NAME__.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace __PROJECT_NAME__.Infrastructure.Services.LoggedUser;

public class LoggedUser : ILoggedUser
{
    private readonly __PROJECT_NAME__DbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;

    public LoggedUser(__PROJECT_NAME__DbContext dbContext, ITokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }

    public async Task<User> Get()
    {
        var token = _tokenProvider.TokenOnRequest();

        var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var identifier = jwtSecurityToken.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value;

        return await _dbContext
            .Users
            .AsNoTracking()
            .FirstAsync(user => user.UserIdentifier == Guid.Parse(identifier));
    }
}
```

### `src/__PROJECT_NAME__.Infrastructure/DataAccess/Repositories/Users/UserRepository.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories.Users;
using Microsoft.EntityFrameworkCore;

namespace __PROJECT_NAME__.Infrastructure.DataAccess.Repositories.Users;

internal class UserRepository : IUserReadOnlyRepository, IUserWriteOnlyRepository
{
    private readonly __PROJECT_NAME__DbContext _dbContext;

    public UserRepository(__PROJECT_NAME__DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(User user) => await _dbContext.Users.AddAsync(user);

    public async Task<bool> ExistActiveUserWithEmail(string email) =>
        await _dbContext.Users.AnyAsync(user => user.Email.Equals(email));

    public async Task<User?> GetByEmail(string email) =>
        await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email.Equals(email));
}
```

### Edit `DataAccess/__PROJECT_NAME__DbContext.cs`

Add the `DbSet` and a unique index on e-mail — the uniqueness check in the use case is a
race, the index is the actual guarantee.

```csharp
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
    }
```

with `using __PROJECT_NAME__.Domain.Entities;` and `using Microsoft.EntityFrameworkCore;`.

### Edit `Infrastructure/DependencyInjectionExtension.cs`

Add to `AddInfrastructure`, before `AddRepositories`:

```csharp
        services.AddScoped<IPasswordEncripter, PasswordEncripter>();
        services.AddScoped<ILoggedUser, LoggedUser>();

        AddToken(services, configuration);
```

Add to `AddRepositories`:

```csharp
        services.AddScoped<IUserReadOnlyRepository, UserRepository>();
        services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
```

Add the new method:

```csharp
    private static void AddToken(IServiceCollection services, IConfiguration configuration)
    {
        var expirationTimeMinutes = configuration.GetValue<uint>("Settings:Jwt:ExpiresMinutes");
        var signingKey = configuration.GetValue<string>("Settings:Jwt:SigningKey");

        services.AddScoped<IAccessTokenGenerator>(_ => new JwtTokenGenerator(expirationTimeMinutes, signingKey!));
    }
```

---

## 5. Application

### `src/__PROJECT_NAME__.Application/UseCases/Users/Register/IRegisterUserUseCase.cs`

```csharp
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;

namespace __PROJECT_NAME__.Application.UseCases.Users.Register;

public interface IRegisterUserUseCase
{
    Task<ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request);
}
```

### `src/__PROJECT_NAME__.Application/UseCases/Users/Register/RegisterUserValidator.cs`

```csharp
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Exception;
using FluentValidation;

namespace __PROJECT_NAME__.Application.UseCases.Users.Register;

public class RegisterUserValidator : AbstractValidator<RequestRegisterUserJson>
{
    public RegisterUserValidator()
    {
        RuleFor(user => user.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_REQUIRED);
        RuleFor(user => user.Email).NotEmpty().WithMessage(ResourceErrorMessages.EMAIL_REQUIRED);
        RuleFor(user => user.Password).NotEmpty().WithMessage(ResourceErrorMessages.PASSWORD_REQUIRED);
        RuleFor(user => user.Password).MinimumLength(6).WithMessage(ResourceErrorMessages.PASSWORD_TOO_SHORT);

        When(user => string.IsNullOrWhiteSpace(user.Email) == false, () =>
        {
            RuleFor(user => user.Email).EmailAddress().WithMessage(ResourceErrorMessages.EMAIL_INVALID);
        });
    }
}
```

### `src/__PROJECT_NAME__.Application/UseCases/Users/Register/RegisterUserUseCase.cs`

```csharp
using AutoMapper;
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories;
using __PROJECT_NAME__.Domain.Repositories.Users;
using __PROJECT_NAME__.Domain.Security.Cryptography;
using __PROJECT_NAME__.Domain.Security.Tokens;
using __PROJECT_NAME__.Exception;
using __PROJECT_NAME__.Exception.ExceptionBase;
using FluentValidation.Results;

namespace __PROJECT_NAME__.Application.UseCases.Users.Register;

public class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserReadOnlyRepository _readOnlyRepository;
    private readonly IUserWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordEncripter _passwordEncripter;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public RegisterUserUseCase(
        IUserReadOnlyRepository readOnlyRepository,
        IUserWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _readOnlyRepository = readOnlyRepository;
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordEncripter = passwordEncripter;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request)
    {
        await Validate(request);

        var user = _mapper.Map<User>(request);
        user.Password = _passwordEncripter.Encrypt(request.Password);
        user.UserIdentifier = Guid.NewGuid();

        await _writeOnlyRepository.Add(user);
        await _unitOfWork.Commit();

        return new ResponseRegisteredUserJson
        {
            Name = user.Name,
            Token = _accessTokenGenerator.Generate(user)
        };
    }

    private async Task Validate(RequestRegisterUserJson request)
    {
        var result = new RegisterUserValidator().Validate(request);

        var emailAlreadyExists = await _readOnlyRepository.ExistActiveUserWithEmail(request.Email);
        if (emailAlreadyExists)
        {
            result.Errors.Add(new ValidationFailure(
                nameof(request.Email), ResourceErrorMessages.EMAIL_ALREADY_REGISTERED));
        }

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
```

### `src/__PROJECT_NAME__.Application/UseCases/Login/DoLogin/IDoLoginUseCase.cs`

```csharp
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;

namespace __PROJECT_NAME__.Application.UseCases.Login.DoLogin;

public interface IDoLoginUseCase
{
    Task<ResponseRegisteredUserJson> Execute(RequestLoginJson request);
}
```

### `src/__PROJECT_NAME__.Application/UseCases/Login/DoLogin/DoLoginUseCase.cs`

> Both the unknown-e-mail and the wrong-password branch throw the same exception. Do not
> split them into distinct messages — that turns the endpoint into an account enumeration oracle.

```csharp
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;
using __PROJECT_NAME__.Domain.Repositories.Users;
using __PROJECT_NAME__.Domain.Security.Cryptography;
using __PROJECT_NAME__.Domain.Security.Tokens;
using __PROJECT_NAME__.Exception.ExceptionBase;

namespace __PROJECT_NAME__.Application.UseCases.Login.DoLogin;

public class DoLoginUseCase : IDoLoginUseCase
{
    private readonly IUserReadOnlyRepository _repository;
    private readonly IPasswordEncripter _passwordEncripter;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public DoLoginUseCase(
        IUserReadOnlyRepository repository,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _repository = repository;
        _passwordEncripter = passwordEncripter;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<ResponseRegisteredUserJson> Execute(RequestLoginJson request)
    {
        var user = await _repository.GetByEmail(request.Email) ?? throw new InvalidLoginException();

        if (_passwordEncripter.Verify(request.Password, user.Password) == false)
        {
            throw new InvalidLoginException();
        }

        return new ResponseRegisteredUserJson
        {
            Name = user.Name,
            Token = _accessTokenGenerator.Generate(user)
        };
    }
}
```

### Edit `Application/AutoMapper/AutoMapping.cs`

In `RequestToEntity()`:

```csharp
        CreateMap<RequestRegisterUserJson, User>()
            .ForMember(dest => dest.Password, config => config.Ignore());
```

### Edit `Application/DependencyInjectionExtension.cs`

In `AddUseCases()`:

```csharp
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IDoLoginUseCase, DoLoginUseCase>();
```

---

## 6. Api

### `src/__PROJECT_NAME__.Api/Token/HttpContextTokenValue.cs`

```csharp
using __PROJECT_NAME__.Domain.Security.Tokens;

namespace __PROJECT_NAME__.Api.Token;

public class HttpContextTokenValue : ITokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTokenValue(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TokenOnRequest()
    {
        var authorization = _httpContextAccessor.HttpContext!.Request.Headers.Authorization.ToString();

        return authorization["Bearer ".Length..].Trim();
    }
}
```

### `src/__PROJECT_NAME__.Api/Controllers/UserController.cs`

```csharp
using __PROJECT_NAME__.Application.UseCases.Users.Register;
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;
using Microsoft.AspNetCore.Mvc;

namespace __PROJECT_NAME__.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterUserJson request,
        [FromServices] IRegisterUserUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }
}
```

### `src/__PROJECT_NAME__.Api/Controllers/LoginController.cs`

```csharp
using __PROJECT_NAME__.Application.UseCases.Login.DoLogin;
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;
using Microsoft.AspNetCore.Mvc;

namespace __PROJECT_NAME__.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] RequestLoginJson request,
        [FromServices] IDoLoginUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Ok(response);
    }
}
```

### Edit `Api/Program.cs`

Replace `builder.Services.AddSwaggerGen();` with the Bearer-aware version so the Swagger UI
gets an **Authorize** button:

```csharp
builder.Services.AddSwaggerGen(config =>
{
    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] then your token.",
        In = ParameterLocation.Header,
        Scheme = "Bearer",
        Type = SecuritySchemeType.ApiKey
    });

    config.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
```

Add after `builder.Services.AddApplication();`:

```csharp
builder.Services.AddScoped<ITokenProvider, HttpContextTokenValue>();
builder.Services.AddHttpContextAccessor();

var signingKey = builder.Configuration.GetValue<string>("Settings:Jwt:SigningKey");

builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(config =>
{
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!))
    };
});
```

Add **before** `app.MapControllers();`, in this order:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

New usings:

```csharp
using __PROJECT_NAME__.Api.Token;
using __PROJECT_NAME__.Domain.Security.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
```

---

## 7. Test utilities

### `tests/CommonTestUtilities/Cryptography/PasswordEncripterBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Security.Cryptography;
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
```

### `tests/CommonTestUtilities/Entities/UserBuilder.cs`

```csharp
using Bogus;
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Enums;

namespace CommonTestUtilities.Entities;

public class UserBuilder
{
    public static User Build(string role = Roles.TEAM_MEMBER)
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, _ => 1)
            .RuleFor(u => u.Name, faker => faker.Person.FirstName)
            .RuleFor(u => u.Email, (faker, user) => faker.Internet.Email(user.Name))
            .RuleFor(u => u.Password, faker => faker.Internet.Password(prefixLength: 8))
            .RuleFor(u => u.UserIdentifier, _ => Guid.NewGuid())
            .RuleFor(u => u.Role, _ => role);
    }
}
```

### `tests/CommonTestUtilities/LoggedUser/LoggedUserBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Services.LoggedUser;
using Moq;

namespace CommonTestUtilities.LoggedUser;

public class LoggedUserBuilder
{
    public static ILoggedUser Build(User user)
    {
        var mock = new Mock<ILoggedUser>();

        mock.Setup(loggedUser => loggedUser.Get()).ReturnsAsync(user);

        return mock.Object;
    }
}
```

### `tests/CommonTestUtilities/Token/JwtTokenGeneratorBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Security.Tokens;
using __PROJECT_NAME__.Infrastructure.Security.Tokens;

namespace CommonTestUtilities.Token;

public class JwtTokenGeneratorBuilder
{
    public static IAccessTokenGenerator Build() =>
        new JwtTokenGenerator(expirationTimeMinutes: 5, signingKey: "test-signing-key-with-enough-length-32b+");
}
```

This makes `CommonTestUtilities` reference `Infrastructure`:

```bash
dotnet add tests/CommonTestUtilities reference src/__PROJECT_NAME__.Infrastructure
```

### `tests/CommonTestUtilities/Repositories/UserReadOnlyRepositoryBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories.Users;
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
```

### `tests/CommonTestUtilities/Repositories/UserWriteOnlyRepositoryBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Repositories.Users;
using Moq;

namespace CommonTestUtilities.Repositories;

public class UserWriteOnlyRepositoryBuilder
{
    public static IUserWriteOnlyRepository Build() => new Mock<IUserWriteOnlyRepository>().Object;
}
```

### `tests/CommonTestUtilities/Requests/RequestRegisterUserJsonBuilder.cs`

```csharp
using Bogus;
using __PROJECT_NAME__.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterUserJsonBuilder
{
    public static RequestRegisterUserJson Build(int passwordLength = 10)
    {
        return new Faker<RequestRegisterUserJson>()
            .RuleFor(r => r.Name, faker => faker.Person.FirstName)
            .RuleFor(r => r.Email, (faker, request) => faker.Internet.Email(request.Name))
            .RuleFor(r => r.Password, faker => faker.Internet.Password(passwordLength));
    }
}
```

### `tests/CommonTestUtilities/Requests/RequestLoginJsonBuilder.cs`

```csharp
using Bogus;
using __PROJECT_NAME__.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestLoginJsonBuilder
{
    public static RequestLoginJson Build()
    {
        return new Faker<RequestLoginJson>()
            .RuleFor(r => r.Email, faker => faker.Internet.Email())
            .RuleFor(r => r.Password, faker => faker.Internet.Password());
    }
}
```

---

## 8. Integration test seeding

### `tests/WebApi.Test/Resources/UserIdentityManager.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;

namespace WebApi.Test.Resources;

public class UserIdentityManager
{
    private readonly User _user;
    private readonly string _password;
    private readonly string _token;

    public UserIdentityManager(User user, string password, string token)
    {
        _user = user;
        _password = password;
        _token = token;
    }

    public string GetName() => _user.Name;
    public string GetEmail() => _user.Email;
    public string GetPassword() => _password;
    public string GetToken() => _token;
}
```

### Replace `tests/WebApi.Test/CustomWebApplicationFactory.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Enums;
using __PROJECT_NAME__.Domain.Security.Cryptography;
using __PROJECT_NAME__.Domain.Security.Tokens;
using __PROJECT_NAME__.Infrastructure.DataAccess;
using CommonTestUtilities.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Test.Resources;

namespace WebApi.Test;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public UserIdentityManager User_Team_Member { get; private set; } = default!;
    public UserIdentityManager User_Admin { get; private set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test")
            .ConfigureServices(services =>
            {
                var provider = services.AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

                services.AddDbContext<__PROJECT_NAME__DbContext>(config =>
                {
                    config.UseInMemoryDatabase("InMemoryDbForTesting");
                    config.UseInternalServiceProvider(provider);
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<__PROJECT_NAME__DbContext>();
                var passwordEncripter = scope.ServiceProvider.GetRequiredService<IPasswordEncripter>();
                var accessTokenGenerator = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>();

                StartDatabase(dbContext, passwordEncripter, accessTokenGenerator);
            });
    }

    private void StartDatabase(
        __PROJECT_NAME__DbContext dbContext,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator)
    {
        User_Team_Member = AddUser(dbContext, passwordEncripter, accessTokenGenerator, Roles.TEAM_MEMBER, id: 1);
        User_Admin = AddUser(dbContext, passwordEncripter, accessTokenGenerator, Roles.ADMIN, id: 2);

        dbContext.SaveChanges();
    }

    private static UserIdentityManager AddUser(
        __PROJECT_NAME__DbContext dbContext,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator,
        string role,
        long id)
    {
        var user = UserBuilder.Build(role);
        user.Id = id;

        var plainPassword = user.Password;
        user.Password = passwordEncripter.Encrypt(plainPassword);

        dbContext.Users.Add(user);

        return new UserIdentityManager(user, plainPassword, accessTokenGenerator.Generate(user));
    }
}
```

> The seeded entities stay available to feature modules: `dotnet-new-crud-module` adds its own
> `AddXxx` call and exposes another identity manager alongside these two.
