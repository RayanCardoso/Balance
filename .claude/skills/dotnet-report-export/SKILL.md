---
name: dotnet-report-export
description: Use when an API endpoint must return a downloadable file rather than JSON - generating Excel (.xlsx) or PDF reports from entity data in a .NET Clean Architecture solution, including the use case, the byte-array contract, the controller action and the content-type handling.
---

# Add Excel and PDF report export

## Overview

Adds a report-generation vertical: a use case that returns `byte[]`, and a controller action
that returns a `File` result with the right content type.

**Core principle: the use case returns bytes, the controller decides the transport.** Keeping
`HttpContext` and content types out of `Application` is what lets the same report be scheduled,
e-mailed or cached later without touching it.

## When to Use

- "Export to Excel", "generate a PDF report", "download a spreadsheet"
- A layered .NET API that already has the entity and repository

**When NOT to use:**
- The client only needs JSON it will render itself
- The report is large enough to need streaming or background generation — this skill builds
  everything in memory

## Packages

```bash
dotnet add src/<Name>.Application package ClosedXML
dotnet add src/<Name>.Application package PDFsharp-MigraDoc
```

`ClosedXML` writes `.xlsx`. `PDFsharp-MigraDoc` builds the document model and renders the PDF.
Both run in-process with no native dependency, which matters for Linux containers and CI.

## Structure

```
Application/UseCases/<Es>/Reports/Excel/IGenerate<Es>ReportExcelUseCase.cs
Application/UseCases/<Es>/Reports/Excel/Generate<Es>ReportExcelUseCase.cs
Application/UseCases/<Es>/Reports/Pdf/IGenerate<Es>ReportPdfUseCase.cs
Application/UseCases/<Es>/Reports/Pdf/Generate<Es>ReportPdfUseCase.cs
Api/Controllers/ReportController.cs
```

Both use cases return `Task<byte[]>` and take whatever filter the report needs
(`DateOnly month`, a date range, an id).

## The Contract

```csharp
public interface IGenerateInvoicesReportExcelUseCase
{
    Task<byte[]> Execute(DateOnly month);
}
```

```csharp
public async Task<byte[]> Execute(DateOnly month)
{
    var loggedUser = await _loggedUser.Get();

    var invoices = await _repository.FilterByMonth(loggedUser, month);

    // An empty report is a valid answer, not an error.
    if (invoices.Count == 0)
    {
        return [];
    }

    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add(month.ToString("Y"));

    // ... headers and rows ...

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);

    return stream.ToArray();
}
```

The repository query filters by `loggedUser` exactly as every other read does. A report is the
easiest place to accidentally dump another user's data, because the output is a file nobody
inspects field by field.

## The Controller

```csharp
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportController : ControllerBase
{
    [HttpGet]
    [Route("excel")]
    [ProducesResponseType(typeof(File), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetExcel(
        [FromServices] IGenerateInvoicesReportExcelUseCase useCase,
        [FromHeader] DateOnly month)
    {
        var file = await useCase.Execute(month);

        if (file.Length == 0)
        {
            return NoContent();
        }

        return File(
            file,
            MediaTypeNames.Application.Octet,
            $"invoices_{month:yyyy-MM}.xlsx");
    }
}
```

Returning `204 No Content` for an empty period is deliberate: a zero-byte `.xlsx` download is
a corrupt file to the user's spreadsheet application.

## Font Embedding for PDF

PDFsharp needs a font resolver, and a font present on a Windows dev machine will not exist in a
Linux container. Embed the font files as resources:

```xml
<ItemGroup>
  <None Remove="UseCases\Invoices\Reports\Pdf\Fonts\Roboto-Regular.ttf" />
  <EmbeddedResource Include="UseCases\Invoices\Reports\Pdf\Fonts\Roboto-Regular.ttf" />
</ItemGroup>
```

then implement `IFontResolver` reading from the assembly manifest. Skipping this produces a
report that works locally and throws in production — the single most common failure of this
feature.

## Register and Verify

```csharp
services.AddScoped<IGenerateInvoicesReportExcelUseCase, GenerateInvoicesReportExcelUseCase>();
services.AddScoped<IGenerateInvoicesReportPdfUseCase, GenerateInvoicesReportPdfUseCase>();
```

Then verify by actually downloading:

```bash
dotnet build
dotnet test
```

Open the produced file. A unit test asserting `result.Length > 0` passes on a corrupt workbook —
byte count is not validity.

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| Returning `IActionResult` from the use case | Drags ASP.NET into `Application`; the report can never be reused |
| Not disposing the workbook or stream | Memory grows under concurrent report requests |
| Returning a zero-byte file instead of 204 | The user downloads a file their spreadsheet app refuses to open |
| Skipping the font resolver | Works on Windows, throws in the Linux container |
| Omitting the `ILoggedUser` filter | The report exports every user's rows |
| Asserting only on length in tests | Passes on corrupt output |

## Related Skills

- `dotnet-new-crud-module` — the module a report reads from
- `dotnet-usecase-tests` — tests for the report use case
