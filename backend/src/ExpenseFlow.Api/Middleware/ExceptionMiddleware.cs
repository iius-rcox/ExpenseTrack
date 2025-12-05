using System.Net;
using System.Text.Json;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Middleware;

/// <summary>
/// Global exception handling middleware that returns RFC 7807 Problem Details responses.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        _logger.LogError(exception,
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}",
            traceId,
            context.Request.Path);

        var statusCode = GetStatusCode(exception);
        var problemDetails = CreateProblemDetails(context, exception, statusCode, traceId);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ArgumentException => (int)HttpStatusCode.BadRequest,
        KeyNotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        InvalidOperationException => (int)HttpStatusCode.Conflict,
        _ => (int)HttpStatusCode.InternalServerError
    };

    private ProblemDetailsResponse CreateProblemDetails(
        HttpContext context,
        Exception exception,
        int statusCode,
        string traceId)
    {
        var problemDetails = new ProblemDetailsResponse
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = GetTitle(statusCode),
            Status = statusCode,
            Instance = context.Request.Path,
            TraceId = traceId
        };

        // Include exception details only in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions = new Dictionary<string, object>
            {
                ["exceptionType"] = exception.GetType().Name,
                ["stackTrace"] = exception.StackTrace ?? string.Empty
            };
        }
        else
        {
            problemDetails.Detail = statusCode == (int)HttpStatusCode.InternalServerError
                ? "An unexpected error occurred. Please try again later."
                : exception.Message;
        }

        return problemDetails;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        500 => "Internal Server Error",
        _ => "Error"
    };
}

/// <summary>
/// Extension methods for registering exception middleware.
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
