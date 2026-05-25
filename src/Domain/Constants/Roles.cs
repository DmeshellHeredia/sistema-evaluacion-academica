namespace SistemaEvaluacionAcademica.Domain.Constants;

public static class Roles
{
    public const string Admin      = "Admin";
    public const string Profesor   = "Profesor";
    public const string Estudiante = "Estudiante";

    public const string AdminOrProfesor             = Admin + "," + Profesor;
    public const string AdminOrEstudiante           = Admin + "," + Estudiante;
    public const string AdminOrProfesorOrEstudiante = Admin + "," + Profesor + "," + Estudiante;
}
