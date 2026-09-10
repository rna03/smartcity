using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/hospitals")]
public sealed class HospitalsController(ISpatialDataQueryService queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PointFeatureDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PointFeatureDto>>> Get(
        CancellationToken cancellationToken) =>
        Ok(await queryService.GetHospitalsAsync(cancellationToken));
}
