namespace SistemaEvaluacionAcademica.API.Middleware;

public class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-ID";
    public const string ItemsKey   = "RequestId";

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault()
                        ?? Guid.NewGuid().ToString("N");

        context.Items[ItemsKey] = requestId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
