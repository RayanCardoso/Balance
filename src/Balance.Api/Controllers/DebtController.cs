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
}
