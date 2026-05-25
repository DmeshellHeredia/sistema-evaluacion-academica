using FluentAssertions;
using SistemaEvaluacionAcademica.Application.Common;

namespace SistemaEvaluacionAcademica.UnitTests.Common;

public class EmailHelperTests
{
    [Theory]
    [InlineData("Juan",   "Pérez",   "juan.perez@academia.com")]
    [InlineData("María",  "Gómez",   "maria.gomez@academia.com")]
    [InlineData("Carlos", "Ruiz",    "carlos.ruiz@academia.com")]
    [InlineData("Alfonso","Herrera", "alfonso.herrera@academia.com")]
    [InlineData("Luis",   "Torres",  "luis.torres@academia.com")]
    public void DeriveStudentEmail_StandardCases_ReturnsCorrectEmail(
        string firstName, string lastName, string expected)
    {
        EmailHelper.DeriveStudentEmail(firstName, lastName).Should().Be(expected);
    }

    [Theory]
    [InlineData("ANA",    "GARCÍA",  "ana.garcia@academia.com")]
    [InlineData("PEDRO",  "LÓPEZ",   "pedro.lopez@academia.com")]
    public void DeriveStudentEmail_Uppercase_LowercasesAll(
        string firstName, string lastName, string expected)
    {
        EmailHelper.DeriveStudentEmail(firstName, lastName).Should().Be(expected);
    }

    [Theory]
    [InlineData("José María", "López García", "josemaria.lopezgarcia@academia.com")]
    [InlineData("Ana  ",      "  Soto",       "ana.soto@academia.com")]
    public void DeriveStudentEmail_WithSpacesOrCompositeNames_RemovesSpaces(
        string firstName, string lastName, string expected)
    {
        EmailHelper.DeriveStudentEmail(firstName, lastName).Should().Be(expected);
    }

    [Theory]
    [InlineData("Ñoño",  "Núñez",  "nono.nunez@academia.com")]
    [InlineData("Ángel", "Martínez", "angel.martinez@academia.com")]
    [InlineData("Ü",     "Ö",       "u.o@academia.com")]
    public void DeriveStudentEmail_WithSpecialChars_NormalizesCorrectly(
        string firstName, string lastName, string expected)
    {
        EmailHelper.DeriveStudentEmail(firstName, lastName).Should().Be(expected);
    }

    [Fact]
    public void DeriveStudentEmail_AlwaysEndsWithDomain()
    {
        var email = EmailHelper.DeriveStudentEmail("Test", "User");
        email.Should().EndWith("@academia.com");
    }

    [Fact]
    public void DeriveStudentEmail_IsDeterministic_SameInputSameOutput()
    {
        var first = EmailHelper.DeriveStudentEmail("Juan", "Pérez");
        var second = EmailHelper.DeriveStudentEmail("Juan", "Pérez");
        first.Should().Be(second);
    }
}
