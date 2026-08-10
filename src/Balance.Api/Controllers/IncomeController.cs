using Balance.Application.UseCases.Incomes.ChangeValue;
using Balance.Application.UseCases.Incomes.GetMonthly;
using Balance.Application.UseCases.Incomes.Register;
using Balance.Application.UseCases.Incomes.RegisterPayment;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class IncomeController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseIncomeSourceJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterIncomeSourceJson request,
        [FromServices] IRegisterIncomeSourceUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpPost("payment")]
    [ProducesResponseType(typeof(ResponseIncomePaymentJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterPayment(
        [FromBody] RequestRegisterIncomePaymentJson request,
        [FromServices] IRegisterIncomePaymentUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpPut("value")]
    [ProducesResponseType(typeof(ResponseIncomeSourceVersionJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeValue(
        [FromBody] RequestChangeIncomeSourceValueJson request,
        [FromServices] IChangeIncomeSourceValueUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Ok(response);
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(ResponseMonthlyIncomeJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthly(
        int year,
        int month,
        [FromServices] IGetMonthlyIncomeUseCase useCase)
    {
        var response = await useCase.Execute(year, month);

        return Ok(response);
    }
}
