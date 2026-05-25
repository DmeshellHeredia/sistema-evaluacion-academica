using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Exceptions;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class GradeServiceTests
{
    private readonly Mock<IUnitOfWork>        _uow       = new();
    private readonly Mock<IGradeRepository>   _gradeRepo = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly GradeService             _sut;

    public GradeServiceTests()
    {
        _uow.Setup(u => u.Grades).Returns(_gradeRepo.Object);
        _uow.Setup(u => u.Students).Returns(_studentRepo.Object);
        _sut = new GradeService(_uow.Object, NullLogger<GradeService>.Instance);
    }


    [Fact]
    public async Task CreateAsync_WhenStudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo
            .Setup(r => r.GetWithGradesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _sut.CreateAsync(
            new CreateGradeDto(Guid.NewGuid(), Guid.NewGuid(), 8.5m, "2025-1", null),
            Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("Estudiante");
        _gradeRepo.Verify(r => r.AddAsync(It.IsAny<Grade>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSectionNotFound_ShouldReturnNotFound()
    {
        var student = TestDataBuilder.CreateStudentWithUser();

        _studentRepo
            .Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, Guid.NewGuid(), 8.5m, "2025-1", null),
            Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("Sección");
    }


    [Fact]
    public async Task CreateAsync_WhenStudentNotEnrolledInSection_ShouldReturnFailure()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var subject = TestDataBuilder.CreateSubject();
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 8.5m, "2025-1", null),
            Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("inscrito");
        _gradeRepo.Verify(r => r.AddAsync(It.IsAny<Grade>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task CreateAsync_WhenProfesorCalificatesSectionAjena_ShouldReturnForbidden()
    {
        var student       = TestDataBuilder.CreateStudentWithUser();
        var profesorId    = Guid.NewGuid();
        var otroProfesorId = Guid.NewGuid();
        var subject       = TestDataBuilder.CreateSubject();
        // Section is owned by otroProfesorId, NOT profesorId
        var section = TestDataBuilder.CreateSection(otroProfesorId, subject.Id, subject: subject);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 8.5m, "2025-1", null),
            profesorId, "Profesor");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.ErrorMessage.Should().Contain("asignadas");
        _gradeRepo.Verify(r => r.AddAsync(It.IsAny<Grade>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenProfesorCalificatesSectionPropia_ShouldReturn201()
    {
        var profesorId = Guid.NewGuid();
        var (student, section) = SetupHappyPathMocks(profesorId: profesorId);

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 9.0m, "2025-1", null),
            profesorId, "Profesor");

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateAsync_WhenAdminCalificatesCualquierSeccion_ShouldReturn201()
    {
        var adminId = Guid.NewGuid();
        var (student, section) = SetupHappyPathMocks(profesorId: Guid.NewGuid()); // owned by someone else

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 8.0m, "2025-1", null),
            adminId, "Admin");

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }


    [Fact]
    public async Task CreateAsync_WhenDuplicateGradeForPeriod_ShouldReturnFailure()
    {
        var student  = TestDataBuilder.CreateStudentWithUser();
        var subject  = TestDataBuilder.CreateSubject();
        var section  = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);
        var existing = TestDataBuilder.CreateGrade(student.Id, subject.Id);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _gradeRepo
            .Setup(r => r.GetByStudentSubjectAndPeriodAsync(student.Id, subject.Id, "2025-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 8.5m, "2025-1", null),
            Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _gradeRepo.Verify(r => r.AddAsync(It.IsAny<Grade>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task CreateAsync_HappyPath_ShouldReturn201WithGradeDto()
    {
        var (student, section) = SetupHappyPathMocks();

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 9.0m, "2025-1", "Excelente"),
            Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        result.Data!.Value.Should().Be(9.0m);
        result.Data.Period.Should().Be("2025-1");
        result.Data.Comments.Should().Be("Excelente");
        result.Data.Category.Should().Be("Excelente");
    }

    [Fact]
    public async Task CreateAsync_HappyPath_ShouldCallAddAsyncAndSaveChanges()
    {
        var (student, section) = SetupHappyPathMocks();

        await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, 8.0m, "2025-1", null),
            Guid.NewGuid(), "Admin");

        _gradeRepo.Verify(r => r.AddAsync(
            It.Is<Grade>(g => g.Value == 8.0m && g.Period == "2025-1" && g.StudentId == student.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(9.0,  "Excelente")]
    [InlineData(7.5,  "Buena")]
    [InlineData(4.0,  "Por mejorar")]
    public async Task CreateAsync_ShouldMapCategoryDescriptionCorrectly(decimal value, string expectedCategory)
    {
        var (student, section) = SetupHappyPathMocks();

        var result = await _sut.CreateAsync(
            new CreateGradeDto(student.Id, section.Id, value, "2025-1", null),
            Guid.NewGuid(), "Admin");

        result.Data!.Category.Should().Be(expectedCategory);
    }


    [Fact]
    public async Task UpdateAsync_WhenGradeNotFound_ShouldReturnNotFound()
    {
        _gradeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Grade?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), 8.0m, null, Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("Calificación");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_HappyPath_ShouldReturnSuccess()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var subject = TestDataBuilder.CreateSubject();
        var grade   = TestDataBuilder.CreateGradeWithNavigation(student, subject, 7.0m);

        _gradeRepo.Setup(r => r.GetByIdAsync(grade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(grade);
        _gradeRepo.Setup(r => r.UpdateAsync(grade, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _gradeRepo.Setup(r => r.GetByStudentWithDetailsAsync(grade.StudentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { grade });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(grade.Id, 9.5m, "Corrección aplicada", Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task UpdateAsync_WhenProfesorUpdatesGradeFromAnotherSection_ShouldReturnForbidden()
    {
        var profesorId     = Guid.NewGuid();
        var otroProfesorId = Guid.NewGuid();
        var sectionId      = Guid.NewGuid();
        var subject        = TestDataBuilder.CreateSubject();
        var student        = TestDataBuilder.CreateStudentWithUser();
        var grade          = TestDataBuilder.CreateGradeWithNavigation(student, subject, 7.0m, sectionId: sectionId);

        // Section is owned by a different professor
        var section = TestDataBuilder.CreateSection(otroProfesorId, subject.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Id", sectionId);

        _gradeRepo.Setup(r => r.GetByIdAsync(grade.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(grade);
        _uow.Setup(u => u.GetSectionByIdAsync(sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.UpdateAsync(grade.Id, 9.0m, null, profesorId, "Profesor");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.ErrorMessage.Should().Contain("secciones");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenProfesorUpdatesGradeFromOwnSection_ShouldReturnSuccess()
    {
        var profesorId = Guid.NewGuid();
        var sectionId  = Guid.NewGuid();
        var subject    = TestDataBuilder.CreateSubject();
        var student    = TestDataBuilder.CreateStudentWithUser();
        var grade      = TestDataBuilder.CreateGradeWithNavigation(student, subject, 7.0m, sectionId: sectionId);

        // Section is owned by profesorId
        var section = TestDataBuilder.CreateSection(profesorId, subject.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Id", sectionId);

        _gradeRepo.Setup(r => r.GetByIdAsync(grade.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(grade);
        _uow.Setup(u => u.GetSectionByIdAsync(sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _gradeRepo.Setup(r => r.UpdateAsync(grade, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _gradeRepo.Setup(r => r.GetByStudentWithDetailsAsync(grade.StudentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { grade });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(grade.Id, 9.0m, "Revisión", profesorId, "Profesor");

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminUpdatesAnyGrade_ShouldReturnSuccess()
    {
        var adminId = Guid.NewGuid();
        var subject = TestDataBuilder.CreateSubject();
        var student = TestDataBuilder.CreateStudentWithUser();
        var grade   = TestDataBuilder.CreateGradeWithNavigation(student, subject, 7.0m);

        _gradeRepo.Setup(r => r.GetByIdAsync(grade.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(grade);
        _gradeRepo.Setup(r => r.UpdateAsync(grade, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _gradeRepo.Setup(r => r.GetByStudentWithDetailsAsync(grade.StudentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { grade });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(grade.Id, 9.5m, null, adminId, "Admin");

        result.IsSuccess.Should().BeTrue();
        // Admin does not load section
        _uow.Verify(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteAsync_WhenGradeNotFound_ShouldReturnNotFound()
    {
        _gradeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Grade?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_HappyPath_ShouldCallDeleteAndSave()
    {
        var grade = TestDataBuilder.CreateGrade();
        _gradeRepo.Setup(r => r.GetByIdAsync(grade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(grade);
        _gradeRepo.Setup(r => r.DeleteAsync(grade, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(grade.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        _gradeRepo.Verify(r => r.DeleteAsync(grade, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetByStudentAsync_ShouldReturnMappedGradeDtos()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var subject = TestDataBuilder.CreateSubject();
        var grades  = new[]
        {
            TestDataBuilder.CreateGradeWithNavigation(student, subject, 8.0m, "2025-1"),
            TestDataBuilder.CreateGradeWithNavigation(student, subject, 9.5m, "2025-2"),
        };

        _gradeRepo.Setup(r => r.GetPagedByStudentAsync(student.Id, 1, 20, null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(((IEnumerable<Grade>)grades, grades.Length));

        var result = await _sut.GetByStudentAsync(student.Id, 1, 20, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Items.Select(d => d.Value).Should().Contain(new[] { 8.0m, 9.5m });
    }

    [Fact]
    public async Task GetByStudentAsync_WhenStudentHasNoGrades_ShouldReturnEmptyList()
    {
        _gradeRepo.Setup(r => r.GetPagedByStudentAsync(It.IsAny<Guid>(), 1, 20, null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(((IEnumerable<Grade>)Array.Empty<Grade>(), 0));

        var result = await _sut.GetByStudentAsync(Guid.NewGuid(), 1, 20, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }


    [Fact]
    public async Task GetStudentAverageAsync_ShouldReturnRepositoryAverage()
    {
        var studentId = Guid.NewGuid();
        _gradeRepo.Setup(r => r.GetStudentAverageAsync(studentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((decimal?)8.75m);

        var result = await _sut.GetStudentAverageAsync(studentId);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(8.75m);
    }

    [Fact]
    public async Task GetStudentAverageAsync_WhenNoGrades_ShouldReturnNull()
    {
        _gradeRepo.Setup(r => r.GetStudentAverageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((decimal?)null);

        var result = await _sut.GetStudentAverageAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeNull();
    }


    [Fact]
    public async Task GetSubjectAverageAsync_ShouldReturnRepositoryAverage()
    {
        var subjectId = Guid.NewGuid();
        _gradeRepo.Setup(r => r.GetSubjectAverageAsync(subjectId, "2025-1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(7.5m);

        var result = await _sut.GetSubjectAverageAsync(subjectId, "2025-1");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(7.5m);
    }


    [Theory]
    [InlineData(9.5,  "Excelente")]
    [InlineData(10.0, "Excelente")]
    [InlineData(9.0,  "Excelente")]
    [InlineData(7.0,  "Buena")]
    [InlineData(8.99, "Buena")]
    [InlineData(0.0,  "Por mejorar")]
    [InlineData(6.99, "Por mejorar")]
    public void Grade_CategoryDescription_ShouldBeCalculatedCorrectly(decimal value, string expectedCategory)
    {
        var grade = TestDataBuilder.CreateGrade(value: value);
        grade.CategoryDescription.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(11.0)]
    [InlineData(-1.0)]
    public void Grade_Constructor_WithInvalidValue_ShouldThrowDomainException(decimal invalid)
    {
        var act = () => TestDataBuilder.CreateGrade(value: invalid);
        act.Should().Throw<DomainException>();
    }


    [Fact]
    public async Task GetBySectionAsync_SectionNotFound_ShouldReturnNotFound()
    {
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.GetBySectionAsync(Guid.NewGuid(), 1, 50, Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetBySectionAsync_ProfesorOwnSection_ShouldReturnGrades()
    {
        var profesorId = Guid.NewGuid();
        var subject    = TestDataBuilder.CreateSubject();
        var section    = TestDataBuilder.CreateSection(profesorId, subject.Id, subject: subject);
        var student    = TestDataBuilder.CreateStudentWithUser();
        var grade      = TestDataBuilder.CreateGradeWithNavigation(student, subject, 8.5m);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);
        _gradeRepo.Setup(r => r.GetPagedBySectionAsync(section.Id, 1, 50, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(((IEnumerable<Grade>)new[] { grade }, 1));

        var result = await _sut.GetBySectionAsync(section.Id, 1, 50, profesorId, "Profesor");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Value.Should().Be(8.5m);
    }

    [Fact]
    public async Task GetBySectionAsync_ProfesorOtherSection_ShouldReturnForbidden()
    {
        var profesorId     = Guid.NewGuid();
        var otroProfesorId = Guid.NewGuid();
        var subject        = TestDataBuilder.CreateSubject();
        var section        = TestDataBuilder.CreateSection(otroProfesorId, subject.Id, subject: subject);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);

        var result = await _sut.GetBySectionAsync(section.Id, 1, 50, profesorId, "Profesor");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.ErrorMessage.Should().Contain("secciones");
    }

    [Fact]
    public async Task GetBySectionAsync_AdminAnySection_ShouldReturnGrades()
    {
        var subject = TestDataBuilder.CreateSubject();
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);
        var student = TestDataBuilder.CreateStudentWithUser();
        var grade   = TestDataBuilder.CreateGradeWithNavigation(student, subject, 9.0m);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);
        _gradeRepo.Setup(r => r.GetPagedBySectionAsync(section.Id, 1, 50, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(((IEnumerable<Grade>)new[] { grade }, 1));

        var result = await _sut.GetBySectionAsync(section.Id, 1, 50, Guid.NewGuid(), "Admin");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }


    private (Student student, SubjectSection section) SetupHappyPathMocks(Guid? profesorId = null)
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var subject = TestDataBuilder.CreateSubject();
        var section = TestDataBuilder.CreateSection(profesorId ?? Guid.NewGuid(), subject.Id, subject: subject);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>())).ReturnsAsync(section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _gradeRepo
            .Setup(r => r.GetByStudentSubjectAndPeriodAsync(
                student.Id, subject.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Grade?)null);
        _gradeRepo.Setup(r => r.AddAsync(It.IsAny<Grade>(), It.IsAny<CancellationToken>()))
                  .Returns<Grade, CancellationToken>((g, _) => Task.FromResult(g));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (student, section);
    }
}
