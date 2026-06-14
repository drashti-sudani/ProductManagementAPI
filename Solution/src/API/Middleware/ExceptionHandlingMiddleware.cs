using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Create a logging scope that includes the request trace identifier for correlation
        using var scope = _logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
        {
            ["TraceId"] = context.TraceIdentifier,
            ["RequestPath"] = context.Request.Path,
            ["Method"] = context.Request.Method
        });

        _logger.LogInformation("Handling request {Method} {Path} TraceId={TraceId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);

        try
        {
            await _next(context);
            _logger.LogInformation("Finished handling request {Method} {Path} TraceId={TraceId} StatusCode={StatusCode}", context.Request.Method, context.Request.Path, context.TraceIdentifier, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception caught by global middleware TraceId={TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        int statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        object errors = exception switch
        {
            ValidationException vex => vex.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }),
            _ => new[] { new { error = exception.Message } }
        };

        var payload = new
        {
            status = statusCode,
            errors,
            traceId = context.TraceIdentifier,
            details = (_env.EnvironmentName == "Development" && statusCode == StatusCodes.Status500InternalServerError) ? exception.ToString() : null
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(payload, options);
        return context.Response.WriteAsync(json);
    }
}
