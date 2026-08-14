using Balance.Application.UseCases.Categories.GetAll;
using Balance.Application.UseCases.Categories.Register;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoryController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseCategoryJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterCategoryJson request,
        [FromServices] IRegisterCategoryUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseCategoriesJson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromServices] IGetAllCategoriesUseCase useCase)
    {
        var response = await useCase.Execute();

        return Ok(response);
    }
}
