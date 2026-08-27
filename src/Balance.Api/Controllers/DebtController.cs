using Balance.Application.UseCases.Debts.Register;
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
}
