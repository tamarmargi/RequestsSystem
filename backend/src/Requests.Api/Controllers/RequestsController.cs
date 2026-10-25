using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Requests.Api.Http;
using Requests.BL.Dtos;
using Requests.BL.Results;
using Requests.BL.Services;

namespace Requests.Api.Controllers;

[ApiController]
[Route("api/requests")]
// No controller-level [Produces("application/json")]: it would override application/problem+json on error responses.
public class RequestsController : ControllerBase
{
    private readonly IRequestQueryService _queryService;
    private readonly IRequestCommandService _commandService;
    private readonly CurrentUserAccessor _currentUser;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public RequestsController(
        IRequestQueryService queryService,
        IRequestCommandService commandService,
        CurrentUserAccessor currentUser,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _queryService = queryService;
        _commandService = commandService;
        _currentUser = currentUser;
        _problemDetailsFactory = problemDetailsFactory;
    }

    /// <summary>Paged, filtered and sorted list of requests.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RequestDto>>> Search(
        [FromQuery] RequestQueryParameters parameters, CancellationToken ct) =>
        Ok(await _queryService.SearchAsync(parameters, ct));

    /// <summary>Global counts per status and per priority.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(RequestStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestStatsDto>> GetStats(CancellationToken ct) =>
        Ok(await _queryService.GetStatsAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> GetById(int id, CancellationToken ct)
    {
        var request = await _queryService.GetByIdAsync(id, ct);
        return request is null ? NotFound() : Ok(request);
    }

    /// <summary>Status change history, newest first.</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<RequestHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RequestHistoryItemDto>>> GetHistory(int id, CancellationToken ct)
    {
        var history = await _queryService.GetHistoryAsync(id, ct);
        return history is null ? NotFound() : Ok(history);
    }

    /// <summary>Changes the status, with optimistic concurrency on <c>version</c>. ChangedBy comes from X-User-Name.</summary>
    [HttpPatch("{id:int}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestDto>> UpdateStatus(int id, UpdateStatusRequest body, CancellationToken ct)
    {
        var result = await _commandService.UpdateStatusAsync(id, body, _currentUser.UserName, ct);

        return result switch
        {
            UpdateStatusResult.Success success => Ok(success.Request),
            UpdateStatusResult.NotFound => NotFound(),
            UpdateStatusResult.InvalidTransition invalid =>
                ProblemResult(StatusCodes.Status400BadRequest, "Invalid status transition", invalid.Message),
            UpdateStatusResult.Conflict conflict =>
                ProblemResult(StatusCodes.Status409Conflict, "The request was modified by another user",
                    "The version you sent is out of date. See 'current' for the latest state.", conflict.Current),
            _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
        };
    }

    /// <summary>
    /// Changes the status of up to 100 requests. Partial success: each item succeeds or fails on its own, and the
    /// response is 200 whenever the request itself is valid. ChangedBy comes from X-User-Name.
    /// </summary>
    [HttpPost("bulk-status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BulkUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkUpdateResultDto>> BulkUpdateStatus(BulkUpdateStatusRequest body, CancellationToken ct) =>
        Ok(await _commandService.BulkUpdateStatusAsync(body, _currentUser.UserName, ct));

    private ObjectResult ProblemResult(int statusCode, string title, string detail, RequestDto? current = null)
    {
        var problem = _problemDetailsFactory.CreateProblemDetails(HttpContext, statusCode, title, detail: detail);
        if (current is not null)
        {
            problem.Extensions["current"] = current;
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
