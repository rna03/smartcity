using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public sealed class IncidentsController(IIncidentService incidentService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<IncidentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> Get(
        CancellationToken cancellationToken) =>
        Ok(await incidentService.GetAllAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType<IncidentCreationResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IncidentCreationResult>> Create(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        if (!IncidentRequestValidation.IsValid(request))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid incident",
                Detail = "Type must be Fire, Medical, Accident or Other; coordinates " +
                         "must be finite and within valid ranges; description may not " +
                         $"exceed {IncidentRequestValidation.MaxDescriptionLength} characters."
            });
        }

        var result = await incidentService.CreateAsync(
            request.Type!.Value,
            request.Latitude!.Value,
            request.Longitude!.Value,
            request.Description,
            cancellationToken);

        return CreatedAtAction(nameof(Get), value: result);
    }
}
