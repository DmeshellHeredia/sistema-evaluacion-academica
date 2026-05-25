using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaEvaluacionAcademica.Application.Common;

public static class EmailHelper
{
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Deriva el correo institucional del estudiante a partir del nombre y apellido.
    /// Regla: [nombre_normalizado].[apellido_normalizado]@academia.com
    /// Normalización: minúsculas, trim, sin tildes, sin caracteres no alfanuméricos.
    /// </summary>
    public static string DeriveStudentEmail(string firstName, string lastName)
    {
        var first = NormalizePart(firstName);
        var last  = NormalizePart(lastName);
        return $"{first}.{last}@academia.com";
    }

    private static string NormalizePart(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var decomposed = input.Trim().ToLowerInvariant()
                              .Normalize(NormalizationForm.FormD);

        var withoutDiacritics = new string(
            decomposed
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

        return NonAlphanumeric.Replace(withoutDiacritics, "");
    }
}
