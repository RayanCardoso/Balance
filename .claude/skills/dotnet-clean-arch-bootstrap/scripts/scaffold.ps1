<#
.SYNOPSIS
    Creates the project/solution skeleton for a .NET 10 Clean Architecture API.

.DESCRIPTION
    Deterministic part of the dotnet-clean-arch-bootstrap skill: creates the 6 src
    projects and 4 test projects, wires project references, and adds NuGet packages.
    Package versions are intentionally NOT pinned - NuGet resolves the latest version
    compatible with net10.0.

    This script writes NO C# logic. Source files come from references/base-files.md.

.EXAMPLE
    .\scaffold.ps1 -Name Billing -Path C:\src\Billing
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
    [string]$Name,

    [string]$Path = ".",

    [string]$Framework = "net10.0",

    # The .NET 10 SDK defaults `dotnet new sln` to the XML `.slnx` format, which needs
    # Visual Studio 17.13+. Classic `.sln` is the safer default for broad tooling.
    [ValidateSet("sln", "slnx")]
    [string]$SolutionFormat = "sln"
)

$ErrorActionPreference = "Stop"

function Invoke-Dotnet {
    param([string[]]$Arguments)
    Write-Host "  dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DotnetSoft {
    # For operations that are allowed to fail (e.g. removing a package that isn't there)
    param([string[]]$Arguments)
    & dotnet @Arguments 2>&1 | Out-Null
}

# --- Preconditions -----------------------------------------------------------

$sdk = (& dotnet --version)
if ($LASTEXITCODE -ne 0) { throw "The .NET SDK was not found on PATH." }
if (-not $sdk.StartsWith("10.")) {
    Write-Warning "Active SDK is $sdk. This template targets $Framework and expects a 10.x SDK."
}

if (-not (Test-Path $Path)) {
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}
$root = (Resolve-Path $Path).Path

if (Test-Path (Join-Path $root "src")) {
    throw "'$root\src' already exists. Refusing to scaffold over an existing solution."
}

Push-Location $root
try {
    Write-Host "Scaffolding $Name in $root ($Framework)" -ForegroundColor Cyan

    New-Item -ItemType Directory -Path "src", "tests" -Force | Out-Null

    # --- Solution ------------------------------------------------------------

    Invoke-Dotnet @("new", "sln", "-n", $Name, "--format", $SolutionFormat)
    Invoke-Dotnet @("new", "gitignore")

    # Resolve the file the template actually produced rather than assuming an extension.
    $solutionFile = (Get-ChildItem -Path "." -Filter "$Name.sln*" -File | Select-Object -First 1).Name
    if (-not $solutionFile) { throw "No solution file was created for '$Name'." }

    # --- src projects --------------------------------------------------------

    $classLibs = @("Domain", "Exception", "Communication", "Infrastructure", "Application")
    foreach ($lib in $classLibs) {
        Invoke-Dotnet @("new", "classlib", "-n", "$Name.$lib", "-o", "src/$Name.$lib", "-f", $Framework)
        # The classlib template ships a placeholder Class1.cs
        Remove-Item "src/$Name.$lib/Class1.cs" -Force -ErrorAction SilentlyContinue
    }

    Invoke-Dotnet @("new", "webapi", "--use-controllers", "-n", "$Name.Api", "-o", "src/$Name.Api", "-f", $Framework)
    # The webapi template ships a WeatherForecast sample; Program.cs is replaced later.
    Get-ChildItem "src/$Name.Api" -Filter "WeatherForecast*.cs" -Recurse |
        Remove-Item -Force -ErrorAction SilentlyContinue
    Remove-Item "src/$Name.Api/Controllers/WeatherForecastController.cs" -Force -ErrorAction SilentlyContinue

    # --- test projects -------------------------------------------------------

    Invoke-Dotnet @("new", "classlib", "-n", "CommonTestUtilities", "-o", "tests/CommonTestUtilities", "-f", $Framework)
    Remove-Item "tests/CommonTestUtilities/Class1.cs" -Force -ErrorAction SilentlyContinue

    foreach ($t in @("UseCases.Test", "Validators.Tests", "WebApi.Test")) {
        Invoke-Dotnet @("new", "xunit", "-n", $t, "-o", "tests/$t", "-f", $Framework)
        Remove-Item "tests/$t/UnitTest1.cs" -Force -ErrorAction SilentlyContinue
    }

    # --- solution membership -------------------------------------------------

    Get-ChildItem -Path "src", "tests" -Filter "*.csproj" -Recurse | ForEach-Object {
        Invoke-Dotnet @("sln", $solutionFile, "add", $_.FullName)
    }

    # --- project references --------------------------------------------------
    # Layering rule (enforced by dotnet-arch-guard):
    #   Domain, Exception, Communication -> no project references
    #   Infrastructure -> Domain
    #   Application    -> Domain, Communication, Exception
    #   Api            -> Application, Infrastructure, Communication, Exception

    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "reference", "src/$Name.Domain")

    Invoke-Dotnet @("add", "src/$Name.Application", "reference",
        "src/$Name.Domain", "src/$Name.Communication", "src/$Name.Exception")

    Invoke-Dotnet @("add", "src/$Name.Api", "reference",
        "src/$Name.Application", "src/$Name.Infrastructure",
        "src/$Name.Communication", "src/$Name.Exception")

    Invoke-Dotnet @("add", "tests/CommonTestUtilities", "reference",
        "src/$Name.Application", "src/$Name.Communication", "src/$Name.Domain")

    Invoke-Dotnet @("add", "tests/UseCases.Test", "reference",
        "src/$Name.Application", "tests/CommonTestUtilities")

    Invoke-Dotnet @("add", "tests/Validators.Tests", "reference",
        "src/$Name.Application", "tests/CommonTestUtilities")

    Invoke-Dotnet @("add", "tests/WebApi.Test", "reference",
        "src/$Name.Api", "src/$Name.Infrastructure", "tests/CommonTestUtilities")

    # --- packages ------------------------------------------------------------

    # The .NET 9+ webapi template brings Microsoft.AspNetCore.OpenApi. This stack
    # documents the API with Swashbuckle instead - drop it to avoid two doc pipelines.
    Invoke-DotnetSoft @("remove", "src/$Name.Api", "package", "Microsoft.AspNetCore.OpenApi")

    Invoke-Dotnet @("add", "src/$Name.Api", "package", "Swashbuckle.AspNetCore")
    Invoke-Dotnet @("add", "src/$Name.Api", "package", "Microsoft.AspNetCore.Authentication.JwtBearer")
    Invoke-Dotnet @("add", "src/$Name.Api", "package", "Microsoft.EntityFrameworkCore.Design")

    Invoke-Dotnet @("add", "src/$Name.Application", "package", "AutoMapper")
    Invoke-Dotnet @("add", "src/$Name.Application", "package", "FluentValidation")

    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "package", "Microsoft.EntityFrameworkCore")
    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "package", "Npgsql.EntityFrameworkCore.PostgreSQL")
    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "package", "Microsoft.Extensions.Configuration.Binder")
    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "package", "Microsoft.Extensions.DependencyInjection.Abstractions")
    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "package", "BCrypt.Net-Next")
    Invoke-Dotnet @("add", "src/$Name.Infrastructure", "package", "System.IdentityModel.Tokens.Jwt")

    Invoke-Dotnet @("add", "tests/CommonTestUtilities", "package", "AutoMapper")
    Invoke-Dotnet @("add", "tests/CommonTestUtilities", "package", "Bogus")
    Invoke-Dotnet @("add", "tests/CommonTestUtilities", "package", "Moq")

    foreach ($t in @("UseCases.Test", "Validators.Tests", "WebApi.Test")) {
        Invoke-Dotnet @("add", "tests/$t", "package", "Shouldly")
    }

    Invoke-Dotnet @("add", "tests/WebApi.Test", "package", "Microsoft.AspNetCore.Mvc.Testing")
    Invoke-Dotnet @("add", "tests/WebApi.Test", "package", "Microsoft.EntityFrameworkCore.InMemory")

    Write-Host ""
    Write-Host "Skeleton ready." -ForegroundColor Green
    Write-Host "It does NOT compile yet - write the source files from references/base-files.md next." -ForegroundColor Yellow
}
finally {
    Pop-Location
}
