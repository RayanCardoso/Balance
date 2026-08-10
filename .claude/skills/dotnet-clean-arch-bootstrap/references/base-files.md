# Base source files

Every file below is written verbatim, with `__PROJECT_NAME__` replaced by the solution
name (e.g. `Billing`). Paths are relative to the solution root.

Nothing here is entity-specific and nothing here is auth-specific — authentication is
added afterwards by the `dotnet-auth-jwt-module` skill.

---

## 1. Exception layer

### `src/__PROJECT_NAME__.Exception/ExceptionBase/__PROJECT_NAME__Exception.cs`

```csharp
namespace __PROJECT_NAME__.Exception.ExceptionBase;

public abstract class __PROJECT_NAME__Exception : SystemException
{
    protected __PROJECT_NAME__Exception(string message) : base(message)
    {
    }

    public abstract int StatusCode { get; }

    public abstract List<string> GetErrors();
}
```

### `src/__PROJECT_NAME__.Exception/ExceptionBase/ErrorOnValidationException.cs`

```csharp
using System.Net;

namespace __PROJECT_NAME__.Exception.ExceptionBase;

public class ErrorOnValidationException : __PROJECT_NAME__Exception
{
    private readonly List<string> _errors;

    public ErrorOnValidationException(List<string> errorMessages) : base(string.Empty)
    {
        _errors = errorMessages;
    }

    public override int StatusCode => (int)HttpStatusCode.BadRequest;

    public override List<string> GetErrors() => _errors;
}
```

### `src/__PROJECT_NAME__.Exception/ExceptionBase/NotFoundException.cs`

```csharp
using System.Net;

namespace __PROJECT_NAME__.Exception.ExceptionBase;

public class NotFoundException : __PROJECT_NAME__Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.NotFound;

    public override List<string> GetErrors() => [Message];
}
```

### `src/__PROJECT_NAME__.Exception/ResourceErrorMessages.cs`

> Hand-written on purpose. The `.Designer.cs` that Visual Studio generates from a `.resx`
> does not exist on a clean `dotnet build` in CI, so the `ResourceManager` is wired up
> directly. To add a message: add the key to **both** `.resx` files, then add one property here.

```csharp
using System.Globalization;
using System.Resources;

namespace __PROJECT_NAME__.Exception;

public static class ResourceErrorMessages
{
    private static readonly ResourceManager _resourceManager = new(
        baseName: "__PROJECT_NAME__.Exception.ResourceErrorMessages",
        assembly: typeof(ResourceErrorMessages).Assembly);

    public static string UNKNOWN_ERROR => Get(nameof(UNKNOWN_ERROR));

    private static string Get(string key) =>
        _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
```

### `src/__PROJECT_NAME__.Exception/ResourceErrorMessages.resx`

> No `.csproj` change needed — the SDK globs `**/*.resx` as `EmbeddedResource` by default.

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="UNKNOWN_ERROR" xml:space="preserve">
    <value>An unknown error has occurred.</value>
  </data>
</root>
```

### `src/__PROJECT_NAME__.Exception/ResourceErrorMessages.pt-BR.resx`

Same file with the four `resheader` blocks, and:

```xml
  <data name="UNKNOWN_ERROR" xml:space="preserve">
    <value>Ocorreu um erro desconhecido.</value>
  </data>
```

---

## 2. Communication layer

### `src/__PROJECT_NAME__.Communication/Responses/ResponseErrorJson.cs`

```csharp
namespace __PROJECT_NAME__.Communication.Responses;

public class ResponseErrorJson
{
    public List<string> ErrorMessages { get; set; }

    public ResponseErrorJson(string errorMessage)
    {
        ErrorMessages = [errorMessage];
    }

    public ResponseErrorJson(List<string> errorMessages)
    {
        ErrorMessages = errorMessages;
    }
}
```

---

## 3. Domain layer

### `src/__PROJECT_NAME__.Domain/Repositories/IUnitOfWork.cs`

```csharp
namespace __PROJECT_NAME__.Domain.Repositories;

public interface IUnitOfWork
{
    Task Commit();
}
```

---

## 4. Infrastructure layer

### `src/__PROJECT_NAME__.Infrastructure/DataAccess/__PROJECT_NAME__DbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace __PROJECT_NAME__.Infrastructure.DataAccess;

public class __PROJECT_NAME__DbContext : DbContext
{
    public __PROJECT_NAME__DbContext(DbContextOptions options) : base(options)
    {
    }

    // DbSet<T> properties are added by dotnet-new-crud-module.
}
```

### `src/__PROJECT_NAME__.Infrastructure/DataAccess/UnitOfWork.cs`

```csharp
using __PROJECT_NAME__.Domain.Repositories;

namespace __PROJECT_NAME__.Infrastructure.DataAccess;

internal class UnitOfWork : IUnitOfWork
{
    private readonly __PROJECT_NAME__DbContext _dbContext;

    public UnitOfWork(__PROJECT_NAME__DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Commit() => await _dbContext.SaveChangesAsync();
}
```

### `src/__PROJECT_NAME__.Infrastructure/Extensions/ConfigurationExtensions.cs`

```csharp
using Microsoft.Extensions.Configuration;

namespace __PROJECT_NAME__.Infrastructure.Extensions;

public static class ConfigurationExtensions
{
    public static bool IsTestEnvironment(this IConfiguration configuration)
    {
        return configuration.GetValue<bool>("InMemoryTest");
    }
}
```

### `src/__PROJECT_NAME__.Infrastructure/Migrations/DataBaseMigration.cs`

```csharp
using __PROJECT_NAME__.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace __PROJECT_NAME__.Infrastructure.Migrations;

public static class DataBaseMigration
{
    public static async Task MigrateDatabase(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<__PROJECT_NAME__DbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
```

### `src/__PROJECT_NAME__.Infrastructure/DependencyInjectionExtension.cs`

> `AddRepositories` is the single registration point that `dotnet-new-crud-module` appends to.

```csharp
using __PROJECT_NAME__.Domain.Repositories;
using __PROJECT_NAME__.Infrastructure.DataAccess;
using __PROJECT_NAME__.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace __PROJECT_NAME__.Infrastructure;

public static class DependencyInjectionExtension
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddRepositories(services);

        if (configuration.IsTestEnvironment() == false)
        {
            AddDbContext(services, configuration);
        }
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Connection");

        services.AddDbContext<__PROJECT_NAME__DbContext>(config => config.UseNpgsql(connectionString));
    }
}
```

---

## 5. Application layer

### `src/__PROJECT_NAME__.Application/AutoMapper/AutoMapping.cs`

```csharp
using AutoMapper;

namespace __PROJECT_NAME__.Application.AutoMapper;

public class AutoMapping : Profile
{
    public AutoMapping()
    {
        RequestToEntity();
        EntityToResponse();
    }

    private void RequestToEntity()
    {
    }

    private void EntityToResponse()
    {
    }
}
```

### `src/__PROJECT_NAME__.Application/DependencyInjectionExtension.cs`

```csharp
using __PROJECT_NAME__.Application.AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace __PROJECT_NAME__.Application;

public static class DependencyInjectionExtension
{
    public static void AddApplication(this IServiceCollection services)
    {
        AddUseCases(services);
        AddAutoMapper(services);
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapping>());
    }

    private static void AddUseCases(IServiceCollection services)
    {
    }
}
```

---

## 6. Api layer

### `src/__PROJECT_NAME__.Api/Filters/ExceptionFilter.cs`

```csharp
using __PROJECT_NAME__.Communication.Responses;
using __PROJECT_NAME__.Exception;
using __PROJECT_NAME__.Exception.ExceptionBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace __PROJECT_NAME__.Api.Filters;

public class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is __PROJECT_NAME__Exception)
        {
            HandleProjectException(context);
        }
        else
        {
            ThrowUnknownError(context);
        }
    }

    private static void HandleProjectException(ExceptionContext context)
    {
        var exception = (__PROJECT_NAME__Exception)context.Exception;
        var errorResponse = new ResponseErrorJson(exception.GetErrors());

        context.HttpContext.Response.StatusCode = exception.StatusCode;
        context.Result = new ObjectResult(errorResponse);
    }

    private static void ThrowUnknownError(ExceptionContext context)
    {
        var errorResponse = new ResponseErrorJson(ResourceErrorMessages.UNKNOWN_ERROR);

        context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Result = new ObjectResult(errorResponse);
    }
}
```

### `src/__PROJECT_NAME__.Api/Middleware/CultureMiddleware.cs`

```csharp
using System.Globalization;

namespace __PROJECT_NAME__.Api.Middleware;

public class CultureMiddleware
{
    private readonly RequestDelegate _next;

    public CultureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var supportedLanguages = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();

        var requestedCulture = context.Request.Headers.AcceptLanguage.FirstOrDefault();

        var cultureInfo = new CultureInfo("en");

        if (string.IsNullOrEmpty(requestedCulture) == false
            && supportedLanguages.Exists(language => language.Name.Equals(requestedCulture)))
        {
            cultureInfo = new CultureInfo(requestedCulture);
        }

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        await _next(context);
    }
}
```

### `src/__PROJECT_NAME__.Api/Program.cs`

> `public partial class Program { }` at the bottom is what lets `WebApplicationFactory<Program>`
> boot the API from `WebApi.Test`. Do not remove it.

```csharp
using __PROJECT_NAME__.Api.Filters;
using __PROJECT_NAME__.Api.Middleware;
using __PROJECT_NAME__.Application;
using __PROJECT_NAME__.Infrastructure;
using __PROJECT_NAME__.Infrastructure.Extensions;
using __PROJECT_NAME__.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMvc(options => options.Filters.Add(typeof(ExceptionFilter)));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CultureMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();

if (builder.Configuration.IsTestEnvironment() == false)
{
    await MigrateDataBase();
}

app.Run();

async Task MigrateDataBase()
{
    await using var scope = app.Services.CreateAsyncScope();

    await DataBaseMigration.MigrateDatabase(scope.ServiceProvider);
}

public partial class Program { }
```

### `src/__PROJECT_NAME__.Api/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "InMemoryTest": false,
  "ConnectionStrings": {
    "Connection": ""
  },
  "Settings": {
    "Jwt": {
      "SigningKey": "",
      "ExpiresMinutes": 60
    }
  }
}
```

### `src/__PROJECT_NAME__.Api/appsettings.Development.json`

> `SigningKey` must be at least 32 bytes for HMAC-SHA256. Generate a real one per environment
> and never commit a production key.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Connection": "Host=localhost;Port=5432;Database=__PROJECT_NAME__;Username=postgres;Password=postgres"
  },
  "Settings": {
    "Jwt": {
      "SigningKey": "CHANGE-ME-local-development-signing-key-32b+",
      "ExpiresMinutes": 60
    }
  }
}
```

### `src/__PROJECT_NAME__.Api/appsettings.Test.json`

> `InMemoryTest: true` is what makes `AddInfrastructure` skip the Npgsql `DbContext`
> registration so `CustomWebApplicationFactory` can substitute the in-memory provider.

```json
{
  "InMemoryTest": true,
  "ConnectionStrings": {
    "Connection": ""
  },
  "Settings": {
    "Jwt": {
      "SigningKey": "test-signing-key-used-only-by-integration-tests",
      "ExpiresMinutes": 60
    }
  }
}
```

---

## 7. Test scaffolding

### `tests/CommonTestUtilities/Mapper/MapperBuilder.cs`

> AutoMapper 14+ requires an `ILoggerFactory` in the `MapperConfiguration` constructor.

```csharp
using AutoMapper;
using __PROJECT_NAME__.Application.AutoMapper;
using Microsoft.Extensions.Logging;

namespace CommonTestUtilities.Mapper;

public class MapperBuilder
{
    public static IMapper Build()
    {
        var loggerFactory = new LoggerFactory();

        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new AutoMapping());
        }, loggerFactory);

        return configuration.CreateMapper();
    }
}
```

`CommonTestUtilities` needs `Microsoft.Extensions.Logging` for this:

```bash
dotnet add tests/CommonTestUtilities package Microsoft.Extensions.Logging
```

### `tests/CommonTestUtilities/Repositories/UnitOfWorkBuilder.cs`

```csharp
using __PROJECT_NAME__.Domain.Repositories;
using Moq;

namespace CommonTestUtilities.Repositories;

public class UnitOfWorkBuilder
{
    public static IUnitOfWork Build() => new Mock<IUnitOfWork>().Object;
}
```

### `tests/WebApi.Test/CustomWebApplicationFactory.cs`

> `dotnet-auth-jwt-module` replaces this file with a version that seeds users and exposes tokens.

```csharp
using __PROJECT_NAME__.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Test;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
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

                StartDatabase(dbContext);
            });
    }

    private static void StartDatabase(__PROJECT_NAME__DbContext dbContext)
    {
        dbContext.SaveChanges();
    }
}
```

### `tests/WebApi.Test/__PROJECT_NAME__ClassFixture.cs`

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApi.Test;

public class __PROJECT_NAME__ClassFixture : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public __PROJECT_NAME__ClassFixture(CustomWebApplicationFactory webApplicationFactory)
    {
        _httpClient = webApplicationFactory.CreateClient();
    }

    protected async Task<HttpResponseMessage> DoPost(
        string requestUri, object request, string token = "", string culture = "en")
    {
        AuthorizeRequest(token);
        ChangeRequestCulture(culture);

        return await _httpClient.PostAsJsonAsync(requestUri, request);
    }

    protected async Task<HttpResponseMessage> DoPut(
        string requestUri, object request, string token = "", string culture = "en")
    {
        AuthorizeRequest(token);
        ChangeRequestCulture(culture);

        return await _httpClient.PutAsJsonAsync(requestUri, request);
    }

    protected async Task<HttpResponseMessage> DoGet(
        string requestUri, string token = "", string culture = "en")
    {
        AuthorizeRequest(token);
        ChangeRequestCulture(culture);

        return await _httpClient.GetAsync(requestUri);
    }

    protected async Task<HttpResponseMessage> DoDelete(
        string requestUri, string token = "", string culture = "en")
    {
        AuthorizeRequest(token);
        ChangeRequestCulture(culture);

        return await _httpClient.DeleteAsync(requestUri);
    }

    private void AuthorizeRequest(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ChangeRequestCulture(string culture)
    {
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture));
    }
}
```

---

## 8. Root files

### `docker-compose.yml`

```yaml
services:
  postgres:
    image: postgres:17-alpine
    container_name: __PROJECT_NAME___postgres
    environment:
      POSTGRES_DB: __PROJECT_NAME__
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

### `README.md`

```markdown
# __PROJECT_NAME__

REST API in .NET 10 following Clean Architecture / DDD.

## Stack

EF Core + PostgreSQL · JWT + BCrypt · AutoMapper · FluentValidation · Swagger
· xUnit + Shouldly + Moq + Bogus

## Layers

| Project | Responsibility |
| --- | --- |
| `__PROJECT_NAME__.Api` | Controllers, filters, middleware, DI composition root |
| `__PROJECT_NAME__.Application` | Use cases, validators, AutoMapper profile |
| `__PROJECT_NAME__.Domain` | Entities, repository interfaces, domain services |
| `__PROJECT_NAME__.Infrastructure` | EF Core, repositories, JWT, cryptography |
| `__PROJECT_NAME__.Communication` | Request/response DTOs |
| `__PROJECT_NAME__.Exception` | Exception hierarchy and message catalogue |

## Getting started

    docker compose up -d
    dotnet ef migrations add InitialCreate --project src/__PROJECT_NAME__.Infrastructure --startup-project src/__PROJECT_NAME__.Api
    dotnet run --project src/__PROJECT_NAME__.Api

Swagger UI: `https://localhost:<port>/swagger`

## Tests

    dotnet test
```
