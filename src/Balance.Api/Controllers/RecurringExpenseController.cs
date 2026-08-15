using Balance.Application.UseCases.RecurringExpenses.Archive;
using Balance.Application.UseCases.RecurringExpenses.ChangeValue;
using Balance.Application.UseCases.RecurringExpenses.GetAll;
using Balance.Application.UseCases.RecurringExpenses.Register;
using Balance.Application.UseCases.RecurringExpenses.RegisterPayment;
using Balance.Application.UseCases.RecurringExpenses.UpdatePayment;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RecurringExpenseController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRecurringExpenseJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        [FromBody] RequestRegisterRecurringExpenseJson request,
        [FromServices] IRegisterRecurringExpenseUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    /// <summary>
    /// Every recurring expense of the user, archived or not - the only surface an archived bill's id
    /// stays reachable through, since the monthly view excludes archived rows by design.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseRecurringExpensesJson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromServices] IGetAllRecurringExpensesUseCase useCase)
    {
        var response = await useCase.Execute();

        return Ok(response);
    }

    [HttpPut("value")]
    [ProducesResponseType(typeof(ResponseRecurringExpenseJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeValue(
        [FromBody] RequestChangeRecurringExpenseValueJson request,
        [FromServices] IChangeRecurringExpenseValueUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Ok(response);
    }

    /// <summary>Archives the recurring expense, or unarchives it when <c>archived</c> is false.</summary>
    [HttpPut("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        [FromServices] IArchiveRecurringExpenseUseCase useCase,
        [FromQuery] bool archived = true)
    {
        await useCase.Execute(id, archived);

        return NoContent();
    }

    /// <summary>Records what the bill actually cost in one month.</summary>
    [HttpPost("payment")]
    [ProducesResponseType(typeof(ResponseRecurringExpensePaymentJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterPayment(
        [FromBody] RequestRegisterRecurringExpensePaymentJson request,
        [FromServices] IRegisterRecurringExpensePaymentUseCase useCase)
    {
        var response = await useCase.Execute(request);

        return Created(string.Empty, response);
    }

    /// <summary>Corrects a recorded payment. Its reference month and version are not moved.</summary>
    [HttpPut("payment/{id:guid}")]
    [ProducesResponseType(typeof(ResponseRecurringExpensePaymentJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePayment(
        [FromRoute] Guid id,
        [FromBody] RequestUpdateRecurringExpensePaymentJson request,
        [FromServices] IUpdateRecurringExpensePaymentUseCase useCase)
    {
        var response = await useCase.Execute(id, request);

        return Ok(response);
    }
}
