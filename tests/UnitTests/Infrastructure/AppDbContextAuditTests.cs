using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.UnitTests.Infrastructure;

public class AppDbContextAuditTests : IDisposable
{
    private readonly AppDbContext _db;

    public AppDbContextAuditTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private static Student BuildStudent() =>
        new("EST-AUDIT-001", Guid.NewGuid(), "Ingeniería en Sistemas", 1);

    [Fact]
    public async Task NewEntity_HasCreatedAtSet()
    {
        var student = BuildStudent();
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        student.CreatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task NewEntity_HasNullUpdatedAt()
    {
        var student = BuildStudent();
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        student.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ModifiedEntity_HasUpdatedAtSet()
    {
        var student = BuildStudent();
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        student.Update("Ingeniería en Software", 2);
        await _db.SaveChangesAsync();

        student.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatedAtIsStable_AfterModification()
    {
        var student = BuildStudent();
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var originalCreatedAt = student.CreatedAt;

        student.Update("Ingeniería en Software", 2);
        await _db.SaveChangesAsync();

        student.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task SoftDelete_UpdatesUpdatedAt()
    {
        var student = BuildStudent();
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        student.UpdatedAt.Should().BeNull("precondition: no update yet");

        student.Deactivate();
        await _db.SaveChangesAsync();

        student.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnchangedEntity_UpdatedAtRemainsNull()
    {
        var student = BuildStudent();
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        await _db.SaveChangesAsync();

        student.UpdatedAt.Should().BeNull();
    }
}
