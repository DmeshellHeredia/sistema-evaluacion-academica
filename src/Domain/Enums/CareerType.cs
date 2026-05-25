namespace SistemaEvaluacionAcademica.Domain.Enums;

public static class CareerTypes
{
    public const string IngenieriaEnSistemas = "Ingeniería en Sistemas";
    public const string Ciberseguridad       = "Ciberseguridad";
    public const string DesarrolloDeSoftware = "Desarrollo de Software";

    public static readonly IReadOnlyList<string> All = new[]
    {
        IngenieriaEnSistemas,
        Ciberseguridad,
        DesarrolloDeSoftware,
    };

    public static bool IsValid(string career) =>
        All.Contains(career, StringComparer.OrdinalIgnoreCase);
}
