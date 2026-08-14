using Balance.Application.UseCases.Dashboard.GetMonthly;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(ResponseMonthlyDashboardJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthly(
        int year,
        int month,
        [FromServices] IGetMonthlyDashboardUseCase useCase)
    {
        var response = await useCase.Execute(year, month);

        return Ok(response);
    }
}
