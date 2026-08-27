using Balance.Application.UseCases.Debts.Archive;
using Balance.Application.UseCases.Debts.GetAll;
using Balance.Application.UseCases.Debts.GetById;
using Balance.Application.UseCases.Debts.Register;
using Balance.Application.UseCases.Debts.RegisterPayment;
using Balance.Application.UseCases.Debts.UpdatePayment;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DebtController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDebtJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterDebtJson request,
        [FromServices] IRegisterDebtUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpPost("payment")]
    [ProducesResponseType(typeof(ResponseDebtPaymentJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterPayment(
        [FromBody] RequestRegisterDebtPaymentJson request,
        [FromServices] IRegisterDebtPaymentUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpPut("payment/{id:guid}")]
    [ProducesResponseType(typeof(ResponseDebtPaymentJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePayment(
        [FromRoute] Guid id,
        [FromBody] RequestUpdateDebtPaymentJson request,
        [FromServices] IUpdateDebtPaymentUseCase useCase)
    {
        var response = await useCase.Execute(id, request);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseDebtJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IGetDebtByIdUseCase useCase)
    {
        var response = await useCase.Execute(id);

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseDebtsJson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromServices] IGetAllDebtsUseCase useCase,
        [FromQuery] Guid? creditorId,
        [FromQuery] Guid? personId,
        [FromQuery] bool includeInactive = false)
    {
        var response = await useCase.Execute(creditorId, personId, includeInactive);

        return Ok(response);
    }

    /// <summary>Archives the debt, or unarchives it when <c>archived</c> is false.</summary>
    [HttpPut("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        [FromServices] IArchiveDebtUseCase useCase,
        [FromQuery] bool archived = true)
    {
        await useCase.Execute(id, archived);

        return NoContent();
    }
}
