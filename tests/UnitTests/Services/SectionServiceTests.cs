using FluentAssertions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Sections;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class SectionServiceTests
{
    private readonly Mock<IUnitOfWork>      _uow       = new();
    private readonly Mock<IGradeRepository> _gradeRepo = new();
    private readonly SectionService         _sut;

    public SectionServiceTests()
    {
        _uow.Setup(u => u.Grades).Returns(_gradeRepo.Object);
        _sut = new SectionService(_uow.Object);
    }


    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPageAndTotalCount()
    {
        var professor = TestDataBuilder.CreateProfesorUser();
        var subject   = TestDataBuilder.CreateSubject();
        var sections  = Enumerable.Range(1, 4).Select(i =>
        {
            var sec = TestDataBuilder.CreateSection(professor.Id, subject: subject, sectionCode: $"S{i}");
            TestDataBuilder.SetProperty(sec, "Professor", professor);
            return sec;
        }).ToList();

        _uow.Setup(u => u.GetAllSectionsPagedAsync(1, 10, null, default))
            .ReturnsAsync(((IEnumerable<SubjectSection>)sections, 30));

        var result = await _sut.GetPagedAsync(1, 10, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(4);
        result.Data.TotalCount.Should().Be(30);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_ClampsPageToMinimum1()
    {
        _uow.Setup(u => u.GetAllSectionsPagedAsync(1, 10, null, default))
            .ReturnsAsync((Enumerable.Empty<SubjectSection>(), 0));

        var result = await _sut.GetPagedAsync(0, 10, null);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.GetAllSectionsPagedAsync(1, 10, null, default), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_WithSearch_PassesSearchToRepository()
    {
        const string search = "IS-MAT";
        _uow.Setup(u => u.GetAllSectionsPagedAsync(1, 20, search, default))
            .ReturnsAsync((Enumerable.Empty<SubjectSection>(), 0));

        var result = await _sut.GetPagedAsync(1, 20, search);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.GetAllSectionsPagedAsync(1, 20, search, default), Times.Once);
    }


    [Fact]
    public async Task UpdateAsync_WhenCapacityBelowEnrolledCount_ShouldReturnFailure()
    {
        var professor = TestDataBuilder.CreateProfesorUser();
        var section   = TestDataBuilder.CreateSection(professorId: professor.Id, capacity: 30);

        // Add 5 active enrollments so ActiveEnrollmentCount == 5
        for (var i = 0; i < 5; i++)
            TestDataBuilder.AddEnrollmentToSection(section, TestDataBuilder.CreateEnrollment(Guid.NewGuid(), section.Id));

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var dto = new UpdateSectionDto(DayOfWeekType.Lunes, "08:00", "10:00", "Presencial", Capacity: 4);

        var result = await _sut.UpdateAsync(section.Id, dto);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("5");
        result.ErrorMessage.Should().Contain("4");
    }

    [Fact]
    public async Task UpdateAsync_WhenCapacityEqualsEnrolledCount_ShouldSucceed()
    {
        var professor = TestDataBuilder.CreateProfesorUser();
        var subject   = TestDataBuilder.CreateSubject();
        var section   = TestDataBuilder.CreateSection(professorId: professor.Id, capacity: 30, subject: subject);
        TestDataBuilder.SetProperty(section, "Professor", professor);

        for (var i = 0; i < 5; i++)
            TestDataBuilder.AddEnrollmentToSection(section, TestDataBuilder.CreateEnrollment(Guid.NewGuid(), section.Id));

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new UpdateSectionDto(DayOfWeekType.Lunes, "08:00", "10:00", "Presencial", Capacity: 5);

        var result = await _sut.UpdateAsync(section.Id, dto);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Capacity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_WhenCapacityAboveEnrolledCount_ShouldSucceed()
    {
        var professor = TestDataBuilder.CreateProfesorUser();
        var subject   = TestDataBuilder.CreateSubject();
        var section   = TestDataBuilder.CreateSection(professorId: professor.Id, capacity: 30, subject: subject);
        TestDataBuilder.SetProperty(section, "Professor", professor);

        for (var i = 0; i < 3; i++)
            TestDataBuilder.AddEnrollmentToSection(section, TestDataBuilder.CreateEnrollment(Guid.NewGuid(), section.Id));

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new UpdateSectionDto(DayOfWeekType.Lunes, "08:00", "10:00", "Presencial", Capacity: 25);

        var result = await _sut.UpdateAsync(section.Id, dto);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Capacity.Should().Be(25);
    }

    [Fact]
    public async Task UpdateAsync_WhenSectionNotFound_ShouldReturn404()
    {
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var dto = new UpdateSectionDto(DayOfWeekType.Lunes, "08:00", "10:00", "Presencial", Capacity: 30);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }


    [Fact]
    public async Task GetStudentsAsync_WhenStudentHasGradeWithComments_ShouldIncludeCommentsInDto()
    {
        var professor  = TestDataBuilder.CreateProfesorUser();
        var subject    = TestDataBuilder.CreateSubject();
        var section    = TestDataBuilder.CreateSection(professor.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Professor", professor);

        var student    = TestDataBuilder.CreateStudentWithUser();
        var enrollment = TestDataBuilder.CreateEnrollment(student.Id, section.Id);
        TestDataBuilder.SetProperty(enrollment, "Student", student);
        TestDataBuilder.AddEnrollmentToSection(section, enrollment);

        var grade = TestDataBuilder.CreateGrade(
            studentId: student.Id,
            subjectId: subject.Id,
            comments: "Excelente trabajo");

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _gradeRepo.Setup(r => r.GetBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { grade });

        var result = await _sut.GetStudentsAsync(section.Id, professor.Id, "Profesor");

        result.IsSuccess.Should().BeTrue();
        var dto = result.Data!.Single();
        dto.CurrentGradeComments.Should().Be("Excelente trabajo");
    }

    [Fact]
    public async Task GetStudentsAsync_WhenStudentHasGradeWithNullComments_ShouldHaveNullCommentsInDto()
    {
        var professor  = TestDataBuilder.CreateProfesorUser();
        var subject    = TestDataBuilder.CreateSubject();
        var section    = TestDataBuilder.CreateSection(professor.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Professor", professor);

        var student    = TestDataBuilder.CreateStudentWithUser();
        var enrollment = TestDataBuilder.CreateEnrollment(student.Id, section.Id);
        TestDataBuilder.SetProperty(enrollment, "Student", student);
        TestDataBuilder.AddEnrollmentToSection(section, enrollment);

        var grade = TestDataBuilder.CreateGrade(
            studentId: student.Id,
            subjectId: subject.Id,
            comments: null);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _gradeRepo.Setup(r => r.GetBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { grade });

        var result = await _sut.GetStudentsAsync(section.Id, professor.Id, "Profesor");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Single().CurrentGradeComments.Should().BeNull();
    }

    [Fact]
    public async Task GetStudentsAsync_WhenStudentHasNoGrade_ShouldHaveNullComments()
    {
        var professor  = TestDataBuilder.CreateProfesorUser();
        var subject    = TestDataBuilder.CreateSubject();
        var section    = TestDataBuilder.CreateSection(professor.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Professor", professor);

        var student    = TestDataBuilder.CreateStudentWithUser();
        var enrollment = TestDataBuilder.CreateEnrollment(student.Id, section.Id);
        TestDataBuilder.SetProperty(enrollment, "Student", student);
        TestDataBuilder.AddEnrollmentToSection(section, enrollment);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _gradeRepo.Setup(r => r.GetBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Enumerable.Empty<Grade>());

        var result = await _sut.GetStudentsAsync(section.Id, professor.Id, "Profesor");

        result.IsSuccess.Should().BeTrue();
        var dto = result.Data!.Single();
        dto.CurrentGrade.Should().BeNull();
        dto.CurrentGradeId.Should().BeNull();
        dto.CurrentGradeComments.Should().BeNull();
    }
}
