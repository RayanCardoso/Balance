using Balance.Application.UseCases.Expenses.GetMonthly;
using Balance.Application.UseCases.Expenses.Register;
using Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExpenseController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseExpenseJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterExpenseJson request,
        [FromServices] IRegisterExpenseUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpPost("installment-plan")]
    [ProducesResponseType(typeof(ResponseInstallmentPlanJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterInstallmentPlan(
        [FromBody] RequestRegisterInstallmentPlanJson request,
        [FromServices] IRegisterInstallmentPlanUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(ResponseMonthlyExpenseJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthly(
        int year,
        int month,
        [FromServices] IGetMonthlyExpenseUseCase useCase)
    {
        var response = await useCase.Execute(year, month);

        return Ok(response);
    }
}
