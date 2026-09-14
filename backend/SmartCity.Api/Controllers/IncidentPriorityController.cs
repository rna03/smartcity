using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;
using SmartCity.Domain;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/location-analysis/incident-priority")]
public sealed class IncidentPriorityController(
    IIncidentPriorityService incidentPriorityService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IncidentPriorityPreviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IncidentPriorityPreviewResponse>> Get(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] IncidentType? incidentType,
        CancellationToken cancellationToken)
    {
        if (!LocationCoordinateValidation.IsValid(latitude, longitude) ||
            !incidentType.HasValue ||
            !Enum.IsDefined(incidentType.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid priority analysis request",
                Detail = "Latitude must be between -90 and 90, longitude must be " +
                         "between -180 and 180, both coordinates must be finite, and " +
                         "incidentType must be Fire, Medical, Accident or Other."
            });
        }

        var analysis = await incidentPriorityService.AnalyzeAsync(
            latitude!.Value,
            longitude!.Value,
            incidentType.Value,
            cancellationToken);
        var priority = analysis.Priority;

        return Ok(new IncidentPriorityPreviewResponse(
            analysis.SelectedLocation,
            analysis.IncidentType,
            priority.Score,
            priority.Level,
            new IncidentPriorityRelevantService(
                priority.RelevantServiceType,
                priority.RelevantServiceDistanceMeters),
            priority.AccessibilityLevel,
            new IncidentPriorityBreakdown(
                priority.IncidentTypeBaseScore,
                priority.ServiceDistanceScore,
                priority.AccessibilityPenalty)));
    }
}
