namespace SistemaEvaluacionAcademica.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IEnumerable<string> Errors { get; private set; } = Enumerable.Empty<string>();
    public int StatusCode { get; private set; }

    private Result() { }

    public static Result<T> Success(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static Result<T> Failure(string error, int statusCode = 400) =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = statusCode };

    public static Result<T> Failure(IEnumerable<string> errors, int statusCode = 422) =>
        new() { IsSuccess = false, Errors = errors, StatusCode = statusCode };

    public static Result<T> NotFound(string error) =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = 404 };

    public static Result<T> Unauthorized(string error = "No autorizado.") =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = 401 };

    public static Result<T> Forbidden(string error = "Acceso denegado.") =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = 403 };
}
