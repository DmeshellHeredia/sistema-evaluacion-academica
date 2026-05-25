using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SistemaEvaluacionAcademica.API.Middleware;

namespace SistemaEvaluacionAcademica.API.Filters;

public class RequestIdFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { StatusCode: >= 400 } objectResult)
            return;

        var requestId = context.HttpContext.Items[RequestIdMiddleware.ItemsKey] as string;
        if (requestId is null)
            return;

        var merged = new Dictionary<string, object?> { ["requestId"] = requestId };

        if (objectResult.Value is not null and not string and not System.Collections.IEnumerable)
        {
            foreach (var prop in objectResult.Value.GetType().GetProperties())
            {
                var key = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                if (!merged.ContainsKey(key))
                    merged[key] = prop.GetValue(objectResult.Value);
            }
        }

        objectResult.Value = merged;
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
