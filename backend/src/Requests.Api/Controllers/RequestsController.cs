using Microsoft.AspNetCore.Mvc;
using Requests.BL.Dtos;
using Requests.BL.Services;

namespace Requests.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public class RequestsController : ControllerBase
{
    private readonly IRequestQueryService _queryService;

    public RequestsController(IRequestQueryService queryService)
    {
        _queryService = queryService;
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
}
