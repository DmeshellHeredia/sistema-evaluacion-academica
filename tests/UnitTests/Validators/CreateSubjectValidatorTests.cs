using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Subjects;
using SistemaEvaluacionAcademica.Application.Validators.Subjects;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.UnitTests.Validators;

public class CreateSubjectValidatorTests
{
    private readonly CreateSubjectValidator _validator = new();

    private static CreateSubjectDto Valid(
        string code = "IS-MAT1",
        string name = "Nombre Válido",
        string desc = "Descripción válida",
        int credits = 4,
        int semesterLevel = 1,
        bool appliesToAllCareers = false,
        IReadOnlyList<string>? careers = null) =>
        new(code, name, desc, credits, semesterLevel, appliesToAllCareers,
            careers ?? new[] { CareerTypes.IngenieriaEnSistemas });


    [Theory]
    [InlineData("MAT101",  "Matemáticas I",   "Cálculo diferencial e integral", 4)]
    [InlineData("PROG201", "Programación I",  "Fundamentos de programación",    5)]
    [InlineData("AB123",   "Materia Corta",   "Descripción de la materia",      1)]
    [InlineData("ABCD999", "Materia Larga",   "Descripción extensa",            10)]
    public async Task Validate_WithValidDto_ShouldPass(string code, string name, string desc, int credits)
    {
        var result = await _validator.ValidateAsync(Valid(code, name, desc, credits));
        result.IsValid.Should().BeTrue();
    }


    [Theory]
    [InlineData("IS-MAT1")]
    [InlineData("CIB-FUN1")]
    [InlineData("DS-PRG1")]
    [InlineData("MAT101")]
    [InlineData("PROG201")]
    [InlineData("BD301")]
    [InlineData("FIS999")]
    public async Task Validate_WithValidCodeFormat_ShouldPass(string code)
    {
        var result = await _validator.ValidateAsync(Valid(code: code));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("mat101")]
    [InlineData("123MAT")]
    [InlineData("")]
    [InlineData("-AAAA1")]
    public async Task Validate_WithInvalidCodeFormat_ShouldFailOnCode(string code)
    {
        var result = await _validator.ValidateAsync(Valid(code: code));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }


    [Fact]
    public async Task Validate_WithEmptyName_ShouldFailOnName()
    {
        var result = await _validator.ValidateAsync(Valid(name: ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameTooShort_ShouldFailOnName()
    {
        var result = await _validator.ValidateAsync(Valid(name: "AB"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExactly3Chars_ShouldPass()
    {
        var result = await _validator.ValidateAsync(Valid(name: "Mat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNameTooLong_ShouldFailOnName()
    {
        var result = await _validator.ValidateAsync(Valid(name: new string('A', 201)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExactly200Chars_ShouldPass()
    {
        var result = await _validator.ValidateAsync(Valid(name: new string('A', 200)));
        result.IsValid.Should().BeTrue();
    }


    [Fact]
    public async Task Validate_WithEmptyDescription_ShouldFailOnDescription()
    {
        var result = await _validator.ValidateAsync(Valid(desc: ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Validate_WithDescriptionTooLong_ShouldFailOnDescription()
    {
        var result = await _validator.ValidateAsync(Valid(desc: new string('X', 1001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Validate_WithDescriptionExactly1000Chars_ShouldPass()
    {
        var result = await _validator.ValidateAsync(Valid(desc: new string('X', 1000)));
        result.IsValid.Should().BeTrue();
    }


    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task Validate_WithCreditsOutOfRange_ShouldFailOnCredits(int credits)
    {
        var result = await _validator.ValidateAsync(Valid(credits: credits));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Credits");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Validate_WithValidCredits_ShouldPass(int credits)
    {
        var result = await _validator.ValidateAsync(Valid(credits: credits));
        result.IsValid.Should().BeTrue();
    }


    [Theory]
    [InlineData("Ingeniería en Sistemas")]
    [InlineData("Ciberseguridad")]
    [InlineData("Desarrollo de Software")]
    public async Task Validate_WithValidCareer_ShouldPass(string career)
    {
        var result = await _validator.ValidateAsync(Valid(careers: new[] { career }));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Administración")]
    [InlineData("sistemas")]
    public async Task Validate_WithInvalidCareer_ShouldFailOnCareers(string career)
    {
        var result = await _validator.ValidateAsync(Valid(careers: new[] { career }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith("Careers"));
    }

    [Fact]
    public async Task Validate_WithNoCareersAndNotAppliesToAll_ShouldFailOnCareers()
    {
        var dto = new CreateSubjectDto("IS-MAT1", "Nombre Válido", "Descripción válida", 4, 1, false, null);
        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Careers");
    }


    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task Validate_WithSemesterLevelOutOfRange_ShouldFail(int semesterLevel)
    {
        var result = await _validator.ValidateAsync(Valid(semesterLevel: semesterLevel));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SemesterLevel");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task Validate_WithValidSemesterLevel_ShouldPass(int semesterLevel)
    {
        var result = await _validator.ValidateAsync(Valid(semesterLevel: semesterLevel));
        result.IsValid.Should().BeTrue();
    }


    [Fact]
    public async Task Validate_WithAppliesToAllCareers_NullCareersShouldPass()
    {
        var dto = new CreateSubjectDto("GEN-ELE1", "Electiva General", "Descripción electiva", 3, 1, true, null);
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAppliesToAllCareers_InvalidCareersListIsIgnored()
    {
        var dto = new CreateSubjectDto("GEN-ELE1", "Electiva General", "Descripción electiva", 3, 1, true,
            new[] { "CarreraInválida" });
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith("Careers"));
    }


    [Fact]
    public async Task Validate_WithAllInvalidFields_ShouldReportMultipleErrors()
    {
        var dto    = new CreateSubjectDto("bad-code", "", "", 0, 0, false, null);
        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
        result.Errors.Select(e => e.PropertyName).Should()
            .Contain("Code")
            .And.Contain("Name")
            .And.Contain("Credits")
            .And.Contain("Careers");
    }
}
