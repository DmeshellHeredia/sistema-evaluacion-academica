using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.Repositories;

/// <summary>
/// Tests de integración de GradeRepository contra SQL Server real (TestContainers).
/// Verifican la lógica de consulta, filtros y cálculos de promedio a nivel de repositorio.
/// </summary>
[Trait("Category", "Integration")]
public class GradeRepositoryTests : IntegrationTestBase, IAsyncLifetime
{
    // IDs de datos sembrados — se resuelven en InitializeAsync
    private Guid _student1Id;   // Juan Pérez — EST-2025-A001
    private Guid _student2Id;   // Carlos Ruiz — EST-2025-A002
    private Guid _mat101Id;     // IS-MAT1 — Matemáticas I (Juan Pérez tiene calificación)
    private Guid _ing401Id;     // IS-ALG1 — Álgebra Lineal (sujeto secundario para tests)

    public GradeRepositoryTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── IAsyncLifetime ───────────────────────────────────────────────────────

    async Task IAsyncLifetime.InitializeAsync()
    {
        // Resolver IDs de los datos sembrados desde la BD del contenedor
        await ExecuteInScopeAsync(async db =>
        {
            var students = await db.Students
                .OrderBy(s => s.StudentCode)
                .Take(2)
                .ToListAsync();

            _student1Id = students[0].Id; // EST-2025-0001
            _student2Id = students[1].Id; // EST-2025-0002

            _mat101Id = (await db.Subjects.FirstAsync(s => s.Code == "IS-MAT1")).Id;
            _ing401Id = (await db.Subjects.FirstAsync(s => s.Code == "IS-ALG1")).Id;

            return true;
        });
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IServiceScope CreateScope() => Factory.Services.CreateScope();

    // ── GetByStudentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStudentAsync_WithSeededStudent_ReturnsOnlyHisActiveGrades()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = await repo.GetByStudentAsync(_student1Id);

        grades.Should().NotBeEmpty();
        grades.Should().OnlyContain(g => g.StudentId == _student1Id);
        grades.Should().OnlyContain(g => g.IsActive);
    }

    [Fact]
    public async Task GetByStudentAsync_WithNonExistentStudent_ReturnsEmptyCollection()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = await repo.GetByStudentAsync(Guid.NewGuid());

        grades.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldIncludeSubjectNavigation()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = (await repo.GetByStudentAsync(_student1Id)).ToList();

        grades.Should().NotBeEmpty();
        grades.Should().AllSatisfy(g => g.Subject.Should().NotBeNull());
        grades.Should().AllSatisfy(g => g.Subject.Code.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnGradesOrderedByPeriodDescending()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = (await repo.GetByStudentAsync(_student1Id)).ToList();

        // Verificar que están ordenadas por Period descendente
        grades.Should().BeInDescendingOrder(g => g.Period);
    }

    // ── GetStudentAverageAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetStudentAverageAsync_WithSeededGrades_MatchesManualCalculation()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        // Juan Pérez tiene: MAT101=9.5, PROG201=8.0, BD301=7.5 → promedio = 8.33
        var grades  = await repo.GetByStudentAsync(_student1Id);
        var expected = Math.Round(grades.Select(g => g.Value).Average(), 2);

        var average = await repo.GetStudentAverageAsync(_student1Id);

        average.Should().Be(expected);
    }

    [Fact]
    public async Task GetStudentAverageAsync_WithNoGrades_ReturnsNull()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var average = await repo.GetStudentAverageAsync(Guid.NewGuid());

        average.Should().BeNull();
    }

    [Fact]
    public async Task GetStudentAverageAsync_ResultIsRoundedToTwoDecimals()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var average = await repo.GetStudentAverageAsync(_student1Id);

        average.Should().HaveValue();
        // El promedio debe tener máximo 2 decimales
        var decimals = BitConverter.GetBytes(decimal.GetBits(average!.Value)[3])[2];
        decimals.Should().BeLessOrEqualTo(2);
    }

    // ── GetBySubjectAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetBySubjectAsync_WithoutPeriodFilter_ReturnsAllActiveGrades()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        // IS-MAT1 tiene calificaciones para estudiantes sembrados
        var grades = await repo.GetBySubjectAsync(_mat101Id);

        grades.Should().NotBeEmpty();
        grades.Should().OnlyContain(g => g.SubjectId == _mat101Id);
        grades.Should().OnlyContain(g => g.IsActive);
    }

    [Fact]
    public async Task GetBySubjectAsync_WithPeriodFilter_ReturnsOnlyMatchingPeriod()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = await repo.GetBySubjectAsync(_mat101Id, period: "2024-1");

        grades.Should().NotBeEmpty();
        grades.Should().OnlyContain(g => g.Period == "2024-1");
        grades.Should().OnlyContain(g => g.SubjectId == _mat101Id);
    }

    [Fact]
    public async Task GetBySubjectAsync_WithNonExistingPeriod_ReturnsEmpty()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = await repo.GetBySubjectAsync(_mat101Id, period: "1900-1");

        grades.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySubjectAsync_ShouldIncludeStudentAndUserNavigation()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = (await repo.GetBySubjectAsync(_mat101Id)).ToList();

        grades.Should().NotBeEmpty();
        grades.Should().AllSatisfy(g =>
        {
            g.Student.Should().NotBeNull();
            g.Student.User.Should().NotBeNull();
            g.Student.StudentCode.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── GetByStudentSubjectAndPeriodAsync ────────────────────────────────────

    [Fact]
    public async Task GetByStudentSubjectAndPeriodAsync_WithExistingRecord_ReturnsGrade()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        // El seeder creó: student1 + IS-MAT1 + "2024-1" → 9.0
        var grade = await repo.GetByStudentSubjectAndPeriodAsync(_student1Id, _mat101Id, "2024-1");

        grade.Should().NotBeNull();
        grade!.StudentId.Should().Be(_student1Id);
        grade.SubjectId.Should().Be(_mat101Id);
        grade.Period.Should().Be("2024-1");
        grade.Value.Should().Be(9.0m);
    }

    [Fact]
    public async Task GetByStudentSubjectAndPeriodAsync_WithWrongPeriod_ReturnsNull()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grade = await repo.GetByStudentSubjectAndPeriodAsync(_student1Id, _mat101Id, "9999-1");

        grade.Should().BeNull();
    }

    [Fact]
    public async Task GetByStudentSubjectAndPeriodAsync_WithWrongStudent_ReturnsNull()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grade = await repo.GetByStudentSubjectAndPeriodAsync(Guid.NewGuid(), _mat101Id, "2025-1");

        grade.Should().BeNull();
    }

    // ── GetSubjectAverageAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetSubjectAverageAsync_ForMAT101Period2025_1_ReturnsCorrectAverage()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        // IS-MAT1 en 2024-1: Juan=9.0 → promedio = 9.0
        var average = await repo.GetSubjectAverageAsync(_mat101Id, "2024-1");

        average.Should().BeInRange(0, 10);
        average.Should().BeApproximately(9.0m, 0.01m);
    }

    [Fact]
    public async Task GetSubjectAverageAsync_WithNonExistingPeriod_ReturnsZero()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var average = await repo.GetSubjectAverageAsync(_mat101Id, "1900-1");

        average.Should().Be(0m);
    }

    // ── GetByStudentWithDetailsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetByStudentWithDetailsAsync_ShouldIncludeAllNavigationProperties()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = (await repo.GetByStudentWithDetailsAsync(_student1Id)).ToList();

        grades.Should().NotBeEmpty();
        grades.Should().AllSatisfy(g =>
        {
            g.Student.Should().NotBeNull();
            g.Student.User.Should().NotBeNull();
            g.Subject.Should().NotBeNull();
            g.GradedByUser.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetByStudentWithDetailsAsync_ShouldOrderByCreatedAtDescending()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGradeRepository>();

        var grades = (await repo.GetByStudentWithDetailsAsync(_student1Id)).ToList();

        grades.Should().BeInDescendingOrder(g => g.CreatedAt);
    }
}
