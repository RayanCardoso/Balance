# CRUD module templates

Substitutions:

| Token | Meaning | Example |
| --- | --- | --- |
| `__PROJECT_NAME__` | Solution name | `Billing` |
| `__E__` | Entity, singular PascalCase | `Invoice` |
| `__ES__` | Entity, plural PascalCase | `Invoices` |
| `__e__` | Entity, singular camelCase | `invoice` |

Examples below assume a user-owned entity. For a shared entity, drop `UserId`/`User` from the
entity, drop the `User user` parameter from every repository signature, and drop `ILoggedUser`
from every use case.

---

## Domain

### `Entities/__E__.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Entities;

public class __E__
{
    public long Id { get; set; }
    // ... entity properties ...

    public long UserId { get; set; }
    public User User { get; set; } = default!;
}
```

### `Repositories/__ES__/I__ES__ReadOnlyRepository.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;

namespace __PROJECT_NAME__.Domain.Repositories.__ES__;

public interface I__ES__ReadOnlyRepository
{
    Task<List<__E__>> GetAll(User user);
    Task<__E__?> GetById(User user, long id);
}
```

### `Repositories/__ES__/I__ES__WriteOnlyRepository.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;

namespace __PROJECT_NAME__.Domain.Repositories.__ES__;

public interface I__ES__WriteOnlyRepository
{
    Task Add(__E__ __e__);
    Task Delete(long id);
}
```

### `Repositories/__ES__/I__ES__UpdateOnlyRepository.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;

namespace __PROJECT_NAME__.Domain.Repositories.__ES__;

public interface I__ES__UpdateOnlyRepository
{
    Task<__E__?> GetById(User user, long id);
    void Update(__E__ __e__);
}
```

---

## Communication

```csharp
namespace __PROJECT_NAME__.Communication.Requests;

public class RequestRegister__E__Json
{
    // ... create payload ...
}
```

```csharp
namespace __PROJECT_NAME__.Communication.Requests;

public class Request__E__Json
{
    // ... update payload ...
}
```

```csharp
namespace __PROJECT_NAME__.Communication.Responses;

public class ResponseRegistered__E__Json
{
    // ... minimal echo of what was created ...
}

public class Response__E__Json
{
    public long Id { get; set; }
    // ... full detail ...
}

public class ResponseShort__E__Json
{
    public long Id { get; set; }
    // ... list-row fields only ...
}

public class Response__ES__Json
{
    public List<ResponseShort__E__Json> __ES__ { get; set; } = [];
}
```

> Never expose `UserId` or the `User` navigation property in a response.

---

## Infrastructure

### `DataAccess/Repositories/__ES__/__ES__Repository.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories.__ES__;
using Microsoft.EntityFrameworkCore;

namespace __PROJECT_NAME__.Infrastructure.DataAccess.Repositories.__ES__;

internal class __ES__Repository :
    I__ES__ReadOnlyRepository,
    I__ES__WriteOnlyRepository,
    I__ES__UpdateOnlyRepository
{
    private readonly __PROJECT_NAME__DbContext _dbContext;

    public __ES__Repository(__PROJECT_NAME__DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(__E__ __e__) => await _dbContext.__ES__.AddAsync(__e__);

    public async Task<List<__E__>> GetAll(User user)
    {
        return await _dbContext.__ES__
            .AsNoTracking()
            .Where(__e__ => __e__.UserId == user.Id)
            .ToListAsync();
    }

    // Read path: no tracking, the entity is projected to a DTO and discarded.
    async Task<__E__?> I__ES__ReadOnlyRepository.GetById(User user, long id)
    {
        return await _dbContext.__ES__
            .AsNoTracking()
            .FirstOrDefaultAsync(__e__ => __e__.Id == id && __e__.UserId == user.Id);
    }

    // Update path: tracked on purpose - SaveChanges must see the mutation.
    async Task<__E__?> I__ES__UpdateOnlyRepository.GetById(User user, long id)
    {
        return await _dbContext.__ES__
            .FirstOrDefaultAsync(__e__ => __e__.Id == id && __e__.UserId == user.Id);
    }

    public void Update(__E__ __e__) => _dbContext.__ES__.Update(__e__);

    public async Task Delete(long id)
    {
        var result = await _dbContext.__ES__.FindAsync(id);

        _dbContext.__ES__.Remove(result!);
    }
}
```

> `Delete` is reached only after the use case has already loaded the entity through the
> ownership-filtered `GetById`. Do not call it from anywhere that has not done that check.

### `DataAccess/__PROJECT_NAME__DbContext.cs`

```csharp
    public DbSet<__E__> __ES__ { get; set; }
```

### `DependencyInjectionExtension.cs` — inside `AddRepositories`

```csharp
        services.AddScoped<I__ES__ReadOnlyRepository, __ES__Repository>();
        services.AddScoped<I__ES__WriteOnlyRepository, __ES__Repository>();
        services.AddScoped<I__ES__UpdateOnlyRepository, __ES__Repository>();
```

---

## Application

### Register

```csharp
namespace __PROJECT_NAME__.Application.UseCases.__ES__.Register;

public interface IRegister__E__UseCase
{
    Task<ResponseRegistered__E__Json> Execute(RequestRegister__E__Json request);
}
```

```csharp
using AutoMapper;
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories;
using __PROJECT_NAME__.Domain.Repositories.__ES__;
using __PROJECT_NAME__.Domain.Services.LoggedUser;
using __PROJECT_NAME__.Exception.ExceptionBase;

namespace __PROJECT_NAME__.Application.UseCases.__ES__.Register;

public class Register__E__UseCase : IRegister__E__UseCase
{
    private readonly I__ES__WriteOnlyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public Register__E__UseCase(
        I__ES__WriteOnlyRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseRegistered__E__Json> Execute(RequestRegister__E__Json request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var __e__ = _mapper.Map<__E__>(request);
        __e__.UserId = loggedUser.Id;

        await _repository.Add(__e__);
        await _unitOfWork.Commit();

        return _mapper.Map<ResponseRegistered__E__Json>(__e__);
    }

    private static void Validate(RequestRegister__E__Json request)
    {
        var result = new Register__E__Validator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
```

```csharp
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Exception;
using FluentValidation;

namespace __PROJECT_NAME__.Application.UseCases.__ES__.Register;

public class Register__E__Validator : AbstractValidator<RequestRegister__E__Json>
{
    public Register__E__Validator()
    {
        // RuleFor(x => x.Title).NotEmpty().WithMessage(ResourceErrorMessages.TITLE_REQUIRED);
    }
}
```

### GetAll

```csharp
public class GetAll__E__UseCase : IGetAll__E__UseCase
{
    private readonly I__ES__ReadOnlyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public GetAll__E__UseCase(
        I__ES__ReadOnlyRepository repository,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _repository = repository;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<Response__ES__Json> Execute()
    {
        var loggedUser = await _loggedUser.Get();

        var result = await _repository.GetAll(loggedUser);

        return new Response__ES__Json
        {
            __ES__ = _mapper.Map<List<ResponseShort__E__Json>>(result)
        };
    }
}
```

### GetById

```csharp
public class Get__E__ByIdUseCase : IGet__E__ByIdUseCase
{
    private readonly I__ES__ReadOnlyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public Get__E__ByIdUseCase(
        I__ES__ReadOnlyRepository repository,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _repository = repository;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<Response__E__Json> Execute(long id)
    {
        var loggedUser = await _loggedUser.Get();

        var result = await _repository.GetById(loggedUser, id);

        if (result is null)
        {
            throw new NotFoundException(ResourceErrorMessages.__E___NOT_FOUND);
        }

        return _mapper.Map<Response__E__Json>(result);
    }
}
```

### Update

```csharp
public class Update__E__UseCase : IUpdate__E__UseCase
{
    private readonly I__ES__UpdateOnlyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public Update__E__UseCase(
        I__ES__UpdateOnlyRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task Execute(long id, Request__E__Json request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var __e__ = await _repository.GetById(loggedUser, id);

        if (__e__ is null)
        {
            throw new NotFoundException(ResourceErrorMessages.__E___NOT_FOUND);
        }

        _mapper.Map(request, __e__);

        _repository.Update(__e__);

        await _unitOfWork.Commit();
    }

    private static void Validate(Request__E__Json request)
    {
        var result = new Update__E__Validator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
```

> `_mapper.Map(request, __e__)` maps **onto** the tracked entity. `_mapper.Map<__E__>(request)`
> would create a detached instance with `Id = 0` and persist nothing.

### Delete

```csharp
public class Delete__E__UseCase : IDelete__E__UseCase
{
    private readonly I__ES__WriteOnlyRepository _writeRepository;
    private readonly I__ES__ReadOnlyRepository _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public Delete__E__UseCase(
        I__ES__WriteOnlyRepository writeRepository,
        I__ES__ReadOnlyRepository readRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task Execute(long id)
    {
        var loggedUser = await _loggedUser.Get();

        // Ownership check before deletion - Delete(id) alone is unscoped.
        var result = await _readRepository.GetById(loggedUser, id);

        if (result is null)
        {
            throw new NotFoundException(ResourceErrorMessages.__E___NOT_FOUND);
        }

        await _writeRepository.Delete(id);

        await _unitOfWork.Commit();
    }
}
```

### `DependencyInjectionExtension.cs` — inside `AddUseCases`

```csharp
        services.AddScoped<IRegister__E__UseCase, Register__E__UseCase>();
        services.AddScoped<IGetAll__E__UseCase, GetAll__E__UseCase>();
        services.AddScoped<IGet__E__ByIdUseCase, Get__E__ByIdUseCase>();
        services.AddScoped<IUpdate__E__UseCase, Update__E__UseCase>();
        services.AddScoped<IDelete__E__UseCase, Delete__E__UseCase>();
```

### `AutoMapper/AutoMapping.cs`

```csharp
    // RequestToEntity()
        CreateMap<RequestRegister__E__Json, __E__>();
        CreateMap<Request__E__Json, __E__>();

    // EntityToResponse()
        CreateMap<__E__, ResponseRegistered__E__Json>();
        CreateMap<__E__, ResponseShort__E__Json>();
        CreateMap<__E__, Response__E__Json>();
```

---

## Api

### `Controllers/__ES__Controller.cs`

```csharp
using __PROJECT_NAME__.Application.UseCases.__ES__.Delete;
using __PROJECT_NAME__.Application.UseCases.__ES__.GetAll;
using __PROJECT_NAME__.Application.UseCases.__ES__.GetById;
using __PROJECT_NAME__.Application.UseCases.__ES__.Register;
using __PROJECT_NAME__.Application.UseCases.__ES__.Update;
using __PROJECT_NAME__.Communication.Requests;
using __PROJECT_NAME__.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace __PROJECT_NAME__.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class __ES__Controller : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegistered__E__Json), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegister__E__Json request,
        [FromServices] IRegister__E__UseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response__ES__Json), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetAll([FromServices] IGetAll__E__UseCase useCase)
    {
        var response = await useCase.Execute();

        if (response.__ES__.Count != 0)
        {
            return Ok(response);
        }

        return NoContent();
    }

    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(Response__E__Json), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromServices] IGet__E__ByIdUseCase useCase,
        [FromRoute] long id)
    {
        var response = await useCase.Execute(id);

        return Ok(response);
    }

    [HttpPut]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromServices] IUpdate__E__UseCase useCase,
        [FromRoute] long id,
        [FromBody] Request__E__Json request)
    {
        await useCase.Execute(id, request);

        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromServices] IDelete__E__UseCase useCase,
        [FromRoute] long id)
    {
        await useCase.Execute(id);

        return NoContent();
    }
}
```

---

## Test builders

### `CommonTestUtilities/Entities/__E__Builder.cs`

```csharp
using Bogus;
using __PROJECT_NAME__.Domain.Entities;

namespace CommonTestUtilities.Entities;

public class __E__Builder
{
    public static __E__ Build(User user)
    {
        return new Faker<__E__>()
            .RuleFor(e => e.Id, _ => 1)
            // ... property rules ...
            .RuleFor(e => e.UserId, _ => user.Id)
            .RuleFor(e => e.User, _ => user);
    }
}
```

### `CommonTestUtilities/Repositories/__ES__ReadOnlyRepositoryBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories.__ES__;
using Moq;

namespace CommonTestUtilities.Repositories;

public class __ES__ReadOnlyRepositoryBuilder
{
    private readonly Mock<I__ES__ReadOnlyRepository> _repository = new();

    public __ES__ReadOnlyRepositoryBuilder GetAll(User user, List<__E__> items)
    {
        _repository.Setup(r => r.GetAll(user)).ReturnsAsync(items);

        return this;
    }

    public __ES__ReadOnlyRepositoryBuilder GetById(User user, __E__? item)
    {
        if (item is not null)
        {
            _repository.Setup(r => r.GetById(user, item.Id)).ReturnsAsync(item);
        }

        return this;
    }

    public I__ES__ReadOnlyRepository Build() => _repository.Object;
}
```

> `GetById` is configured only for the matching user and id. A test that executes as a
> different user falls through to the default `null` — which is exactly how the ownership
> test proves the filter works.

### `CommonTestUtilities/Repositories/__ES__WriteOnlyRepositoryBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Repositories.__ES__;
using Moq;

namespace CommonTestUtilities.Repositories;

public class __ES__WriteOnlyRepositoryBuilder
{
    public static I__ES__WriteOnlyRepository Build() => new Mock<I__ES__WriteOnlyRepository>().Object;
}
```

### `CommonTestUtilities/Repositories/__ES__UpdateOnlyRepositoryBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Entities;
using __PROJECT_NAME__.Domain.Repositories.__ES__;
using Moq;

namespace CommonTestUtilities.Repositories;

public class __ES__UpdateOnlyRepositoryBuilder
{
    private readonly Mock<I__ES__UpdateOnlyRepository> _repository = new();

    public __ES__UpdateOnlyRepositoryBuilder GetById(User user, __E__ item)
    {
        _repository.Setup(r => r.GetById(user, item.Id)).ReturnsAsync(item);

        return this;
    }

    public I__ES__UpdateOnlyRepository Build() => _repository.Object;
}
```

---

## Error messages

Add to both `.resx` files and `ResourceErrorMessages.cs`:

| Key | en | pt-BR |
| --- | --- | --- |
| `__E___NOT_FOUND` | `__E__` not found. | `__E__` não encontrado. |

Plus one key per validation rule in the two validators.
