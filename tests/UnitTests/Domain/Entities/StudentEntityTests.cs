using FluentAssertions;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Domain.Entities;

public class StudentEntityTests
{

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var code     = "EST-2025-0001";
        var userId   = Guid.NewGuid();
        var career   = "Ingeniería en Sistemas";
        var semester = 5;

        var student = TestDataBuilder.CreateStudent(code, userId, career, semester);

        student.StudentCode.Should().Be(code);
        student.UserId.Should().Be(userId);
        student.Career.Should().Be(career);
        student.Semester.Should().Be(semester);
        student.Id.Should().NotBeEmpty();
        student.IsActive.Should().BeTrue();
    }


    [Fact]
    public void CalculateOverallAverage_WithNoGrades_ShouldReturnNull()
    {
        var student = TestDataBuilder.CreateStudent();

        var result = student.CalculateOverallAverage();

        result.Should().BeNull();
    }

    [Fact]
    public void CalculateOverallAverage_WithSingleGrade_ShouldReturnThatValue()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var grade   = TestDataBuilder.CreateGrade(studentId: student.Id, value: 8.5m);
        TestDataBuilder.AddGrades(student, grade);

        var result = student.CalculateOverallAverage();

        result.Should().Be(8.5m);
    }

    [Fact]
    public void CalculateOverallAverage_WithMultipleGrades_ShouldReturnRoundedAverage()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var g1 = TestDataBuilder.CreateGrade(studentId: student.Id, value: 8.0m);
        var g2 = TestDataBuilder.CreateGrade(studentId: student.Id, value: 9.0m);
        var g3 = TestDataBuilder.CreateGrade(studentId: student.Id, value: 7.5m);
        TestDataBuilder.AddGrades(student, g1, g2, g3);

        var result = student.CalculateOverallAverage();

        // (8.0 + 9.0 + 7.5) / 3 = 8.1666... → 8.17
        result.Should().Be(8.17m);
    }

    [Fact]
    public void CalculateOverallAverage_ShouldIgnoreInactiveGrades()
    {
        var student  = TestDataBuilder.CreateStudentWithUser();
        var active   = TestDataBuilder.CreateGrade(studentId: student.Id, value: 9.0m);
        var inactive = TestDataBuilder.CreateGrade(studentId: student.Id, value: 2.0m);
        inactive.Deactivate();

        TestDataBuilder.AddGrades(student, active, inactive);

        var result = student.CalculateOverallAverage();

        // Solo cuenta el grado activo (9.0)
        result.Should().Be(9.0m);
    }

    [Fact]
    public void CalculateOverallAverage_WhenAllGradesInactive_ShouldReturnNull()
    {
        var student  = TestDataBuilder.CreateStudentWithUser();
        var inactive = TestDataBuilder.CreateGrade(studentId: student.Id, value: 8.0m);
        inactive.Deactivate();
        TestDataBuilder.AddGrades(student, inactive);

        var result = student.CalculateOverallAverage();

        result.Should().BeNull();
    }

    [Fact]
    public void CalculateOverallAverage_WithPerfectScores_ShouldReturn10()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var g1 = TestDataBuilder.CreateGrade(studentId: student.Id, value: 10.0m);
        var g2 = TestDataBuilder.CreateGrade(studentId: student.Id, value: 10.0m);
        TestDataBuilder.AddGrades(student, g1, g2);

        student.CalculateOverallAverage().Should().Be(10.0m);
    }


    [Fact]
    public void Update_ShouldChangeCareerAndSemester()
    {
        var student = TestDataBuilder.CreateStudent(career: "Administración", semester: 2);

        student.Update("Ingeniería Civil", 6);

        student.Career.Should().Be("Ingeniería Civil");
        student.Semester.Should().Be(6);
    }

    [Fact]
    public void Update_ShouldNotChangeStudentCode()
    {
        var code    = "EST-2025-0001";
        var student = TestDataBuilder.CreateStudent(studentCode: code);
        student.Update("Nueva Carrera", 4);

        student.StudentCode.Should().Be(code);
    }


    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        var student = TestDataBuilder.CreateStudent();
        student.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_ShouldSetIsActiveFalse()
    {
        var student = TestDataBuilder.CreateStudent();

        student.Deactivate();

        student.IsActive.Should().BeFalse();
    }
}
