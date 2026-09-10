using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/import/openstreetmap")]
public sealed class OpenStreetMapImportController(
    IImportOpenStreetMapDataService importService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OpenStreetMapImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OpenStreetMapImportResult>> Import(
        CancellationToken cancellationToken)
    {
        var result = await importService.ImportAsync(cancellationToken);
        return Ok(result);
    }
}
