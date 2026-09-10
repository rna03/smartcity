using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartCity.Application.Exceptions;

namespace SmartCity.Api.ExceptionHandling;

internal sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            ExternalDataSourceException => StatusCodes.Status502BadGateway,
            DataPersistenceException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        LogRequestFailure(logger, exception, statusCode);
        httpContext.Response.StatusCode = statusCode;

        var detail = environment.IsDevelopment()
            ? exception.Message
            : "The request could not be completed. Check the server logs for details.";

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    StatusCodes.Status502BadGateway => "OpenStreetMap service unavailable",
                    StatusCodes.Status503ServiceUnavailable => "Database unavailable",
                    _ => "Unexpected server error"
                },
                Detail = detail
            }
        });
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Request failed with status code {StatusCode}")]
    private static partial void LogRequestFailure(
        ILogger logger,
        Exception exception,
        int statusCode);
}
