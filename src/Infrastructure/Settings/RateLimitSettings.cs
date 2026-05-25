namespace SistemaEvaluacionAcademica.Infrastructure.Settings;

public sealed class RateLimitSettings
{
    public int LoginPermitLimit { get; set; } = 5;
}
