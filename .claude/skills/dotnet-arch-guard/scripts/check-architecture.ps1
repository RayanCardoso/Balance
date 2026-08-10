<#
.SYNOPSIS
    Checks the structural rules of a .NET Clean Architecture solution.

.DESCRIPTION
    Reports only mechanical violations - things provable from the files themselves.
    Judgment calls (is this logic in the right layer? is this abstraction useful?)
    are left to the reviewing agent; see SKILL.md.

    Exit code 0 = no violations, 1 = violations found.

.EXAMPLE
    .\check-architecture.ps1 -Root C:\src\Billing
#>
param(
    [string]$Root = "."
)

$ErrorActionPreference = "Stop"

$Root = (Resolve-Path $Root).Path
$findings = New-Object System.Collections.ArrayList

function Add-Finding {
    param([string]$Rule, [string]$File, [string]$Message)
    [void]$findings.Add([PSCustomObject]@{ Rule = $Rule; File = $File; Message = $Message })
}

# Discover the solution name from the src layout: src/<Name>.Domain
$domainProject = Get-ChildItem "$Root\src" -Filter "*.Domain" -Directory -ErrorAction SilentlyContinue |
    Select-Object -First 1
if (-not $domainProject) {
    Write-Error "No 'src\*.Domain' project found under '$Root'. Is this the solution root?"
    exit 2
}
$name = $domainProject.Name -replace '\.Domain$', ''

Write-Host "Checking $name in $Root" -ForegroundColor Cyan
Write-Host ""

# --- Rule 1: project reference layering -------------------------------------

$allowed = @{
    "$name.Domain"         = @()
    "$name.Exception"      = @()
    "$name.Communication"  = @()
    "$name.Infrastructure" = @("$name.Domain")
    "$name.Application"    = @("$name.Domain", "$name.Communication", "$name.Exception")
    "$name.Api"            = @("$name.Application", "$name.Infrastructure", "$name.Communication", "$name.Exception")
}

foreach ($project in $allowed.Keys) {
    $csproj = "$Root\src\$project\$project.csproj"
    if (-not (Test-Path $csproj)) { continue }

    $xml = [xml](Get-Content $csproj -Raw)
    $refs = $xml.Project.ItemGroup.ProjectReference |
        Where-Object { $_ -ne $null } |
        ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Include) }

    foreach ($ref in $refs) {
        if ($allowed[$project] -notcontains $ref) {
            Add-Finding "layering" "src\$project\$project.csproj" `
                "$project must not reference $ref"
        }
    }
}

# --- Rule 2: Application must not use Infrastructure namespaces -------------

$appDir = "$Root\src\$name.Application"
if (Test-Path $appDir) {
    Get-ChildItem $appDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object {
            $hits = Select-String -Path $_.FullName -Pattern "^\s*using\s+$([regex]::Escape($name))\.Infrastructure"
            foreach ($hit in $hits) {
                Add-Finding "layering" $_.FullName.Replace("$Root\", "") `
                    "line $($hit.LineNumber): Application imports Infrastructure"
            }
        }
}

# --- Rule 3: every use case is registered in DI -----------------------------

$appDi = "$appDir\DependencyInjectionExtension.cs"
if (Test-Path $appDi) {
    $diText = Get-Content $appDi -Raw

    Get-ChildItem $appDir -Filter "*UseCase.cs" -Recurse |
        Where-Object { $_.Name -notlike "I*UseCase.cs" -and $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object {
            $class = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            if ($diText -notmatch "\b$([regex]::Escape($class))\b") {
                Add-Finding "di-registration" $_.FullName.Replace("$Root\", "") `
                    "$class is never registered in AddUseCases - fails at runtime, not build"
            }
        }
}

# --- Rule 4: every repository interface is registered in DI -----------------

$infraDi = "$Root\src\$name.Infrastructure\DependencyInjectionExtension.cs"
$repoDir = "$Root\src\$name.Domain\Repositories"
if ((Test-Path $infraDi) -and (Test-Path $repoDir)) {
    $diText = Get-Content $infraDi -Raw

    Get-ChildItem $repoDir -Filter "I*Repository.cs" -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object {
            $iface = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            if ($diText -notmatch "\b$([regex]::Escape($iface))\b") {
                Add-Finding "di-registration" $_.FullName.Replace("$Root\", "") `
                    "$iface is never registered in AddRepositories"
            }
        }
}

# --- Rule 5: controller actions document their responses --------------------

$controllerDir = "$Root\src\$name.Api\Controllers"
if (Test-Path $controllerDir) {
    Get-ChildItem $controllerDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object {
            $lines = Get-Content $_.FullName
            $verbs = ($lines | Select-String -Pattern '^\s*\[Http(Get|Post|Put|Delete|Patch)').Count
            $produces = ($lines | Select-String -Pattern '^\s*\[ProducesResponseType').Count

            if ($verbs -gt 0 -and $produces -lt $verbs) {
                Add-Finding "swagger" $_.FullName.Replace("$Root\", "") `
                    "$verbs action(s) but only $produces ProducesResponseType attribute(s)"
            }
        }
}

# --- Rule 6: controllers must not touch data access directly ----------------

if (Test-Path $controllerDir) {
    Get-ChildItem $controllerDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object {
            $hits = Select-String -Path $_.FullName -Pattern "DbContext|IQueryable|\.SaveChanges"
            foreach ($hit in $hits) {
                Add-Finding "layering" $_.FullName.Replace("$Root\", "") `
                    "line $($hit.LineNumber): controller reaches into data access - belongs in a use case"
            }
        }
}

# --- Report -----------------------------------------------------------------

if ($findings.Count -eq 0) {
    Write-Host "No structural violations found." -ForegroundColor Green
    exit 0
}

$findings | Group-Object Rule | ForEach-Object {
    Write-Host "[$($_.Name)]" -ForegroundColor Yellow
    $_.Group | ForEach-Object {
        Write-Host "  $($_.File)"
        Write-Host "    $($_.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "$($findings.Count) violation(s)." -ForegroundColor Red
exit 1
