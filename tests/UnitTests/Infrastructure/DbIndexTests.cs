using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.UnitTests.Infrastructure;

/// <summary>
/// Verifies EF Core model metadata contains the expected database indices.
/// Uses InMemory provider — no Docker needed. Checks both pre-existing and new indices.
/// </summary>
public class DbIndexTests : IDisposable
{
    private readonly AppDbContext _db;

    public DbIndexTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private IEnumerable<IReadOnlyList<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyProperty>> GetIndexColumns<T>()
        where T : class
    {
        var entityType = _db.Model.FindEntityType(typeof(T))!;
        return entityType.GetIndexes().Select(ix => ix.Properties);
    }

    private bool HasIndex<T>(params string[] columnNames) where T : class
    {
        var set = columnNames.ToHashSet();
        return GetIndexColumns<T>().Any(props =>
            props.Count == columnNames.Length &&
            props.Select(p => p.Name).ToHashSet().SetEquals(set));
    }

    private bool HasUniqueIndex<T>(params string[] columnNames) where T : class
    {
        var set = columnNames.ToHashSet();
        return _db.Model.FindEntityType(typeof(T))!.GetIndexes()
            .Any(ix => ix.IsUnique &&
                       ix.Properties.Count == columnNames.Length &&
                       ix.Properties.Select(p => p.Name).ToHashSet().SetEquals(set));
    }


    [Fact]
    public void Users_Email_HasUniqueIndex() =>
        HasUniqueIndex<User>("Email").Should().BeTrue();

    [Fact]
    public void Students_StudentCode_HasUniqueIndex() =>
        HasUniqueIndex<Student>("StudentCode").Should().BeTrue();

    [Fact]
    public void Subjects_Code_HasUniqueIndex() =>
        HasUniqueIndex<Subject>("Code").Should().BeTrue();

    [Fact]
    public void AcademicPeriods_Code_HasUniqueIndex() =>
        HasUniqueIndex<AcademicPeriod>("Code").Should().BeTrue();

    [Fact]
    public void SectionEnrollments_StudentId_SectionId_HasUniqueIndex() =>
        HasUniqueIndex<SectionEnrollment>("StudentId", "SectionId").Should().BeTrue();

    [Fact]
    public void SubjectSections_SubjectId_SectionCode_HasUniqueIndex() =>
        HasUniqueIndex<SubjectSection>("SubjectId", "SectionCode").Should().BeTrue();

    [Fact]
    public void Grades_StudentId_SubjectId_Period_HasUniqueIndex() =>
        HasUniqueIndex<Grade>("StudentId", "SubjectId", "Period").Should().BeTrue();


    [Fact]
    public void SectionEnrollments_StudentId_IsActive_HasIndex() =>
        HasIndex<SectionEnrollment>("StudentId", "IsActive").Should().BeTrue(
            because: "active enrollment lookups by student filter on (StudentId, IsActive)");

    [Fact]
    public void SubjectSections_SubjectId_IsActive_HasIndex() =>
        HasIndex<SubjectSection>("SubjectId", "IsActive").Should().BeTrue(
            because: "active section lookups by subject filter on (SubjectId, IsActive)");

    [Fact]
    public void SectionEnrollments_StudentId_IsActive_IsNotUnique()
    {
        var entityType = _db.Model.FindEntityType(typeof(SectionEnrollment))!;
        var index = entityType.GetIndexes()
            .First(ix => ix.Properties.Count == 2 &&
                         ix.Properties.Any(p => p.Name == "StudentId") &&
                         ix.Properties.Any(p => p.Name == "IsActive"));
        index.IsUnique.Should().BeFalse();
    }

    [Fact]
    public void SubjectSections_SubjectId_IsActive_IsNotUnique()
    {
        var entityType = _db.Model.FindEntityType(typeof(SubjectSection))!;
        var index = entityType.GetIndexes()
            .First(ix => ix.Properties.Count == 2 &&
                         ix.Properties.Any(p => p.Name == "SubjectId") &&
                         ix.Properties.Any(p => p.Name == "IsActive"));
        index.IsUnique.Should().BeFalse();
    }
}
