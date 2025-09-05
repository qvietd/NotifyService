using FluentValidation;
using System.Net;
using System.Text.Json;

namespace NotifyService.Api.Middleware;

public class ExceptionMiddleware
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                response.Message = "Validation failed";
                response.Details = validationEx.Errors.Select(e => e.ErrorMessage).ToArray();
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
            
            case KeyNotFoundException:
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
            
            case ArgumentException:
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
            
            default:
                response.Message = "An error occurred while processing your request";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string[] Details { get; set; } = Array.Empty<string>();
}