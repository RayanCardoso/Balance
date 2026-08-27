using Balance.Application.UseCases.Creditors.Archive;
using Balance.Application.UseCases.Creditors.GetAll;
using Balance.Application.UseCases.Creditors.GetSummary;
using Balance.Application.UseCases.Creditors.Register;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CreditorController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseCreditorJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterCreditorJson request,
        [FromServices] IRegisterCreditorUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseCreditorsJson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromServices] IGetAllCreditorsUseCase useCase,
        [FromQuery] bool includeArchived = false)
    {
        var response = await useCase.Execute(includeArchived);

        return Ok(response);
    }

    /// <summary>Archives the creditor, or unarchives it when <c>archived</c> is false.</summary>
    [HttpPut("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        [FromServices] IArchiveCreditorUseCase useCase,
        [FromQuery] bool archived = true)
    {
        await useCase.Execute(id, archived);

        return NoContent();
    }

    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(ResponseCreditorSummaryJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary(
        [FromRoute] Guid id,
        [FromServices] IGetCreditorSummaryUseCase useCase)
    {
        var response = await useCase.Execute(id);

        return Ok(response);
    }
}
