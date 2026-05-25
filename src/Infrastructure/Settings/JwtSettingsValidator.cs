namespace SistemaEvaluacionAcademica.Infrastructure.Settings;

public static class JwtSettingsValidator
{
    // Valores que indican que nadie configuró el secreto real
    private static readonly HashSet<string> KnownPlaceholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHANGE_ME_USE_ENVIRONMENT_VARIABLE",
        "CHANGE_ME"
    };

    /// <summary>
    /// Valida que JwtSettings.SecretKey sea apta para uso.
    /// En producción además rechaza placeholders conocidos.
    /// </summary>
    /// <exception cref="InvalidOperationException">Cuando la clave es inválida.</exception>
    public static void Validate(string? secretKey, bool isProduction)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                "JwtSettings:SecretKey no está configurado. " +
                "Establece la variable de entorno JwtSettings__SecretKey.");

        if (secretKey.Length < 32)
            throw new InvalidOperationException(
                $"JwtSettings:SecretKey debe tener al menos 32 caracteres (actual: {secretKey.Length}). " +
                "Genera uno con: openssl rand -base64 48");

        if (isProduction && KnownPlaceholders.Contains(secretKey))
            throw new InvalidOperationException(
                "JwtSettings:SecretKey contiene un valor placeholder inseguro en Production. " +
                "Establece un secreto real mediante la variable de entorno JwtSettings__SecretKey.");
    }
}
