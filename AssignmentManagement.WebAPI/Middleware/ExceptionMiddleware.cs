using System.Text.Json;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.WebAPI.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var statusCode = exception switch
            {
                AppException appException => appException.StatusCode,
                DbUpdateException => StatusCodes.Status409Conflict,
                ArgumentException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            if (statusCode >= 500)
                _logger.LogError(exception, "Unhandled server error. TraceId: {TraceId}", context.TraceIdentifier);
            else
                _logger.LogWarning(exception, "Request failed with status {StatusCode}. TraceId: {TraceId}", statusCode, context.TraceIdentifier);

            var message = exception switch
            {
                AppException => exception.Message,
                DbUpdateException => "The requested operation conflicts with existing data.",
                _ when statusCode < 500 => exception.Message,
                _ => $"An unexpected server error occurred. TraceId: {context.TraceIdentifier}"
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var body = GeneralResponse<object>.Fail(message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}
