using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/roads")]
public sealed class RoadsController(ISpatialDataQueryService queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoadFeatureDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoadFeatureDto>>> Get(
        CancellationToken cancellationToken) =>
        Ok(await queryService.GetRoadsAsync(cancellationToken));
}
