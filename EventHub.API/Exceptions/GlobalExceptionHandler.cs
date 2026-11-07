using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace EventHub.API.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                exception,
                "A database concurrency conflict occurred. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            var concurrencyDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict.",
                Detail = "The data was modified by another operation. Please retry your request."
            };

            concurrencyDetails.Extensions["traceId"] =
                httpContext.TraceIdentifier;

            httpContext.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await httpContext.Response.WriteAsJsonAsync(
                concurrencyDetails,
                cancellationToken);

            return true;
        }

        var isDuplicateEvent =
            exception is DbUpdateException dbUpdateException &&
            IsDuplicateEventConstraintViolation(dbUpdateException);

        if (isDuplicateEvent)
        {
            _logger.LogWarning(
                exception,
                "A duplicate event was rejected by the database. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            var conflictDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate event.",
                Detail = "An identical event already exists."
            };

            conflictDetails.Extensions["traceId"] =
                httpContext.TraceIdentifier;

            httpContext.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await httpContext.Response.WriteAsJsonAsync(
                conflictDetails,
                cancellationToken);

            return true;
        }

        _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "An error occurred while processing your request."
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static bool IsDuplicateEventConstraintViolation(
        DbUpdateException exception)
    {
        var sqlException = exception.InnerException as SqlException;

        if (sqlException is null)
            return false;

        var isUniqueViolation =
            sqlException.Number is 2601 or 2627;

        var isEventFingerprintIndex =
            sqlException.Message.Contains(
                "IX_Events_DuplicateFingerprint",
                StringComparison.OrdinalIgnoreCase);

        return isUniqueViolation && isEventFingerprintIndex;
    }
}
