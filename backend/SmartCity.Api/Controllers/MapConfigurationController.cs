using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmartCity.Application.Configuration;
using SmartCity.Application.Models;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/map/config")]
public sealed class MapConfigurationController(IOptions<PilotAreaOptions> pilotAreaOptions)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MapConfigurationDto>(StatusCodes.Status200OK)]
    public ActionResult<MapConfigurationDto> Get() =>
        Ok(MapConfigurationDto.From(pilotAreaOptions.Value));
}
