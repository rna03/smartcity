using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Models;

namespace SmartCity.Api.Controllers;

[ApiController]
[Route("api/location-analysis")]
public sealed class LocationAnalysisController(
    ILocationAnalysisService locationAnalysisService,
    ICoverageAnalysisService coverageAnalysisService,
    IAccessibilityAnalysisService accessibilityAnalysisService)
    : ControllerBase
{
    [HttpGet("nearest")]
    [ProducesResponseType<NearestEmergencyServicesResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NearestEmergencyServicesResult>> GetNearest(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        CancellationToken cancellationToken)
    {
        if (!LocationCoordinateValidation.IsValid(latitude, longitude))
        {
            return BadRequest(CreateInvalidCoordinatesProblem());
        }

        var result = await locationAnalysisService.FindNearestAsync(
            latitude!.Value,
            longitude!.Value,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("coverage")]
    [ProducesResponseType<CoverageAnalysisResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CoverageAnalysisResult>> GetCoverage(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        CancellationToken cancellationToken)
    {
        if (!LocationCoordinateValidation.IsValid(latitude, longitude))
        {
            return BadRequest(CreateInvalidCoordinatesProblem());
        }

        var result = await coverageAnalysisService.AnalyzeAsync(
            latitude!.Value,
            longitude!.Value,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("accessibility")]
    [ProducesResponseType<AccessibilityAnalysisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AccessibilityAnalysisResponse>> GetAccessibility(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        CancellationToken cancellationToken)
    {
        if (!LocationCoordinateValidation.IsValid(latitude, longitude))
        {
            return BadRequest(CreateInvalidCoordinatesProblem());
        }

        var result = await accessibilityAnalysisService.AnalyzeAsync(
            latitude!.Value,
            longitude!.Value,
            cancellationToken);
        return Ok(result);
    }

    private static ProblemDetails CreateInvalidCoordinatesProblem() =>
        new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid coordinates",
            Detail = "Latitude must be between -90 and 90 and longitude " +
                     "must be between -180 and 180. Both values must be finite."
        };
}
