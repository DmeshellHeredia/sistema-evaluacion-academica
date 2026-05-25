using System.Net;
using System.Text.Json;
using SistemaEvaluacionAcademica.Domain.Exceptions;

namespace SistemaEvaluacionAcademica.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var requestId = context.Items[RequestIdMiddleware.ItemsKey] as string
                            ?? context.TraceIdentifier;

            _logger.LogError(ex,
                "Excepción no manejada. Método={Method} Ruta={Path} RequestId={RequestId} Mensaje={Message}",
                context.Request.Method, context.Request.Path,
                requestId, ex.Message);

            await HandleExceptionAsync(context, ex, requestId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
    {
        var (statusCode, message) = exception switch
        {
            ConcurrencyException cEx => (HttpStatusCode.Conflict, cEx.Message),
            DomainException domainEx => (HttpStatusCode.BadRequest, domainEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Acceso denegado."),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error interno del servidor.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            StatusCode = (int)statusCode,
            Message = message,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
