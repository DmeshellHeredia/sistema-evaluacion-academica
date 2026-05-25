using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.UnitTests.Infrastructure;

public class GlobalQueryFilterTests : IDisposable
{
    private readonly AppDbContext _db;

    public GlobalQueryFilterTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();


    [Fact]
    public async Task Student_Active_AppearsInDefaultQuery()
    {
        var userId  = Guid.NewGuid();
        var student = new Student("EST-TEST-001", userId, "Ingeniería en Sistemas", 1);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var result = await _db.Students.ToListAsync();

        result.Should().ContainSingle(s => s.StudentCode == "EST-TEST-001");
    }

    [Fact]
    public async Task Student_SoftDeleted_HiddenFromDefaultQuery()
    {
        var userId  = Guid.NewGuid();
        var student = new Student("EST-TEST-002", userId, "Ingeniería en Sistemas", 1);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        student.Deactivate();
        await _db.SaveChangesAsync();

        var result = await _db.Students.ToListAsync();

        result.Should().NotContain(s => s.StudentCode == "EST-TEST-002");
    }

    [Fact]
    public async Task Student_SoftDeleted_VisibleWithIgnoreQueryFilters()
    {
        var userId  = Guid.NewGuid();
        var student = new Student("EST-TEST-003", userId, "Ingeniería en Sistemas", 1);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        student.Deactivate();
        await _db.SaveChangesAsync();

        var result = await _db.Students.IgnoreQueryFilters().ToListAsync();

        result.Should().Contain(s => s.StudentCode == "EST-TEST-003");
    }


    [Fact]
    public async Task Subject_SoftDeleted_HiddenFromDefaultQuery()
    {
        var subject = new Subject("TST001", "Materia Test", "Descripción test", 3, "Ingeniería en Sistemas", 1);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        subject.Deactivate();
        await _db.SaveChangesAsync();

        var result = await _db.Subjects.ToListAsync();

        result.Should().NotContain(s => s.Code == "TST001");
    }

    [Fact]
    public async Task Subject_SoftDeleted_VisibleWithIgnoreQueryFilters()
    {
        var subject = new Subject("TST002", "Materia Test 2", "Descripción test 2", 3, "Ingeniería en Sistemas", 1);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        subject.Deactivate();
        await _db.SaveChangesAsync();

        var result = await _db.Subjects.IgnoreQueryFilters().ToListAsync();

        result.Should().Contain(s => s.Code == "TST002");
    }


    [Fact]
    public async Task User_SoftDeleted_HiddenFromDefaultQuery()
    {
        var roleId = Guid.NewGuid();
        var user   = new User("test@test.com", "hashed", "Test", "User", roleId);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        user.Deactivate();
        await _db.SaveChangesAsync();

        var result = await _db.Users.ToListAsync();

        result.Should().NotContain(u => u.Email == "test@test.com");
    }


    [Fact]
    public void AllBaseEntityTypes_HaveIsActiveQueryFilter()
    {
        var model = _db.Model;

        var baseEntityTypes = model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType) && !t.IsOwned())
            .ToList();

        baseEntityTypes.Should().NotBeEmpty();

        foreach (var entityType in baseEntityTypes)
        {
            entityType.GetQueryFilter().Should()
                .NotBeNull(because: $"{entityType.ClrType.Name} must have a global IsActive query filter");
        }
    }
}
