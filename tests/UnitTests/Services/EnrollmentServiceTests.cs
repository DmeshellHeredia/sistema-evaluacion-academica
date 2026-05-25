using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Enrollments;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Application.Settings;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IUnitOfWork>        _uow         = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly Mock<ISubjectRepository> _subjectRepo = new();
    private readonly EnrollmentService        _sut;

    public EnrollmentServiceTests()
    {
        _uow.Setup(u => u.Students).Returns(_studentRepo.Object);
        _uow.Setup(u => u.Subjects).Returns(_subjectRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.IsEnrollmentOpenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // Explicit defaults for the serializable transaction block so success tests don't rely on Moq loose mode
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.GetEnrolledSectionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SubjectSection>());
        _sut = new EnrollmentService(_uow.Object, NullLogger<EnrollmentService>.Instance,
            Options.Create(new AcademicSettings()));
    }

    private EnrollmentService CreateServiceWithLimit(int maxCredits) =>
        new(_uow.Object, NullLogger<EnrollmentService>.Instance,
            Options.Create(new AcademicSettings { MaxCreditsPerPeriod = maxCredits }));


    [Fact]
    public async Task EnrollAsync_WithNoPrerequisites_ShouldSucceed()
    {
        var (student, subject, section) = SetupMatchingCareer(studentSemester: 1, subjectSemesterLevel: 1);

        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.AddSectionEnrollmentAsync(It.IsAny<SectionEnrollment>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_WithPrerequisitePassed_ShouldSucceed()
    {
        var prereqSubject = TestDataBuilder.CreateSubject(
            code: "IS-PRG1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var prereqGrade = TestDataBuilder.CreateGrade(student.Id, prereqSubject.Id, 8.5m, "2024-1");
        TestDataBuilder.AddGradeToStudent(student, prereqGrade);

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-PRG2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, prereqSubject.Id) });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereqSubject });

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_MultiplePrerequisites_OneMissing_ShouldReturnFailure()
    {
        var prereq1 = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var prereq2 = TestDataBuilder.CreateSubject(
            code: "IS-PROG1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 3);

        // Student passed prereq1 (8.0 >= 7) but has no grade for prereq2
        TestDataBuilder.AddGradeToStudent(student,
            TestDataBuilder.CreateGrade(student.Id, prereq1.Id, 8.0m, "2024-1"));

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-REDES", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 3);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new[]
            {
                new SubjectPrerequisite(subject.Id, prereq1.Id),
                new SubjectPrerequisite(subject.Id, prereq2.Id),
            });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereq1, prereq2 });

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("IS-PROG1");   // missing prereq named
        result.ErrorMessage.Should().NotContain("IS-MAT1"); // passed prereq not in error
    }


    [Fact]
    public async Task EnrollAsync_StudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo.Setup(r => r.GetWithGradesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Student?)null);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(Guid.NewGuid(), Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task EnrollAsync_SectionNotFound_ShouldReturnNotFound()
    {
        var student = TestDataBuilder.CreateStudentWithUser(career: CareerTypes.IngenieriaEnSistemas);
        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task EnrollAsync_WrongCareer_ShouldReturnFailure()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.Ciberseguridad, semester: 1);
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, default)).ReturnsAsync(section);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("carrera");
    }

    [Fact]
    public async Task EnrollAsync_SemesterTooLow_ShouldReturnFailure()
    {
        var (student, _, section) = SetupMatchingCareer(studentSemester: 1, subjectSemesterLevel: 2);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("semestre");
    }

    [Fact]
    public async Task EnrollAsync_AlreadyEnrolledInSection_ShouldReturnFailure()
    {
        var (student, _, section) = SetupMatchingCareer(studentSemester: 1, subjectSemesterLevel: 1);

        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(true);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inscrito");
    }

    [Fact]
    public async Task EnrollAsync_AlreadyApproved_ShouldReturnFailure()
    {
        var (student, subject, section) = SetupMatchingCareer(studentSemester: 1, subjectSemesterLevel: 1);

        var approvedGrade = TestDataBuilder.CreateGrade(student.Id, subject.Id, 9.0m, "2024-1");
        TestDataBuilder.AddGradeToStudent(student, approvedGrade);

        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("aprobó");
    }

    [Fact]
    public async Task EnrollAsync_PrerequisiteNotPassed_ShouldReturnFailure()
    {
        var prereqSubject = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);
        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, prereqSubject.Id) });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereqSubject });

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Prerequisito");
    }

    [Fact]
    public async Task EnrollAsync_PrerequisiteGradedBelow7_ShouldReturnFailure()
    {
        var prereqSubject = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var failGrade = TestDataBuilder.CreateGrade(student.Id, prereqSubject.Id, 6.0m, "2024-1");
        TestDataBuilder.AddGradeToStudent(student, failGrade);

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, prereqSubject.Id) });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereqSubject });

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Prerequisito");
    }

    [Fact]
    public async Task EnrollAsync_PrerequisiteSubjectSoftDeleted_ShouldReturnFailure()
    {
        // Prerequisite is configured in SubjectPrerequisite, but the prerequisite subject
        // is deactivated — GetByIdsAsync returns empty (EF global filter excludes IsActive=false).
        // The old code did `continue`, silently skipping the check and allowing enrollment.
        // The fix adds it to `missing` and blocks enrollment.
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);
        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);
        var deactivatedPrereqId = Guid.NewGuid();

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(subject.Id, default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, deactivatedPrereqId) });
        // Prerequisite subject excluded by EF global filter — deactivated row not returned
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Prerequisito");
        _uow.Verify(u => u.AddSectionEnrollmentAsync(It.IsAny<SectionEnrollment>(), default), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_PrerequisiteGradeExactly7_ShouldSucceed()
    {
        // Grade of exactly 7.0 is the minimum passing threshold — must be allowed.
        var prereqSubject = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var exactPassGrade = TestDataBuilder.CreateGrade(student.Id, prereqSubject.Id, 7.0m, "2024-1");
        TestDataBuilder.AddGradeToStudent(student, exactPassGrade);

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, prereqSubject.Id) });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereqSubject });

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }


    [Fact]
    public async Task UnenrollAsync_WhenEnrolledWithNoGrade_ShouldSucceed()
    {
        var student   = TestDataBuilder.CreateStudentWithUser(career: CareerTypes.IngenieriaEnSistemas);
        var subject   = TestDataBuilder.CreateSubject();
        var sectionId = Guid.NewGuid();
        var section   = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Id", sectionId);

        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, default)).ReturnsAsync(true);
        _uow.Setup(u => u.GetSectionByIdAsync(sectionId, default)).ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);

        var result = await _sut.UnenrollAsync(student.Id, sectionId);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.RemoveSectionEnrollmentAsync(student.Id, sectionId, default), Times.Once);
    }

    [Fact]
    public async Task UnenrollAsync_WhenNotEnrolled_ShouldReturnFailure()
    {
        _uow.Setup(u => u.IsEnrolledInSectionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync(false);

        var result = await _sut.UnenrollAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inscrito");
    }

    [Fact]
    public async Task UnenrollAsync_WhenHasGradeForSubject_ShouldReturnFailure()
    {
        var student   = TestDataBuilder.CreateStudentWithUser(career: CareerTypes.IngenieriaEnSistemas);
        var subject   = TestDataBuilder.CreateSubject();
        var sectionId = Guid.NewGuid();
        var section   = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Id", sectionId);

        var grade = TestDataBuilder.CreateGrade(student.Id, subject.Id, 8.0m);
        TestDataBuilder.AddGradeToStudent(student, grade);

        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, default)).ReturnsAsync(true);
        _uow.Setup(u => u.GetSectionByIdAsync(sectionId, default)).ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);

        var result = await _sut.UnenrollAsync(student.Id, sectionId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("calificación");
    }


    [Fact]
    public async Task GetCatalogAsync_StudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo.Setup(r => r.GetWithGradesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Student?)null);

        var result = await _sut.GetCatalogAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetCatalogAsync_ShouldClassifySubjectsCorrectly()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var sub1 = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var sub2 = TestDataBuilder.CreateSubject(
            code: "IS-PRG1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var sub3 = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);

        // sub1: approved
        TestDataBuilder.AddGradeToStudent(student,
            TestDataBuilder.CreateGrade(student.Id, sub1.Id, 9.0m, "2024-1"));

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSubjectsByCareerAsync(CareerTypes.IngenieriaEnSistemas, default))
            .ReturnsAsync(new[] { sub1, sub2, sub3 });

        // sub2 is enrolled (returned by GetByStudentAsync)
        _subjectRepo.Setup(r => r.GetByStudentAsync(student.Id, default))
            .ReturnsAsync(new[] { sub2 });

        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<SubjectSection>());

        var result = await _sut.GetCatalogAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        var items = result.Data!.ToList();

        items.Should().HaveCount(3);
        items.First(i => i.SubjectId == sub1.Id).Status.Should().Be(EnrollmentStatus.Aprobada);
        items.First(i => i.SubjectId == sub2.Id).Status.Should().Be(EnrollmentStatus.Inscrita);
        items.First(i => i.SubjectId == sub3.Id).Status.Should().Be(EnrollmentStatus.Disponible);
    }

    [Fact]
    public async Task GetCatalogAsync_SubjectWithUnpassedPrereq_ShouldBeBlocked()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var prereqSub = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSubjectsByCareerAsync(CareerTypes.IngenieriaEnSistemas, default))
            .ReturnsAsync(new[] { prereqSub, subject });
        _subjectRepo.Setup(r => r.GetByStudentAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<Subject>());

        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, prereqSub.Id) });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereqSub });
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<SubjectSection>());

        var result = await _sut.GetCatalogAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        var blocked = result.Data!.First(i => i.SubjectId == subject.Id);
        blocked.Status.Should().Be(EnrollmentStatus.Bloqueada);
        blocked.MissingPrerequisites.Should().HaveCount(1);
        blocked.MissingPrerequisites[0].Code.Should().Be("IS-MAT1");
    }

    [Fact]
    public async Task GetCatalogAsync_SubjectWithPassedPrereq_ShouldBeAvailable()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var prereqSub = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);

        // Student has passing grade for the prerequisite
        TestDataBuilder.AddGradeToStudent(student,
            TestDataBuilder.CreateGrade(student.Id, prereqSub.Id, 8.0m, "2024-1"));

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSubjectsByCareerAsync(CareerTypes.IngenieriaEnSistemas, default))
            .ReturnsAsync(new[] { prereqSub, subject });
        _subjectRepo.Setup(r => r.GetByStudentAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, prereqSub.Id) });
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { prereqSub });
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<SubjectSection>());

        var result = await _sut.GetCatalogAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        var item = result.Data!.First(i => i.SubjectId == subject.Id);
        item.Status.Should().Be(EnrollmentStatus.Disponible);
        item.MissingPrerequisites.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCatalogAsync_PrereqSubjectSoftDeleted_ShouldBeBlocked()
    {
        // The SubjectPrerequisite row exists but the prerequisite subject itself
        // is soft-deleted and excluded by the EF global filter (not returned by GetByIdsAsync).
        // The catalog must treat this as a missing prerequisite and mark the subject Bloqueada.
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 2);

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT2", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 2);
        var deactivatedPrereqId = Guid.NewGuid();

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSubjectsByCareerAsync(CareerTypes.IngenieriaEnSistemas, default))
            .ReturnsAsync(new[] { subject });
        _subjectRepo.Setup(r => r.GetByStudentAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(new[] { new SubjectPrerequisite(subject.Id, deactivatedPrereqId) });
        // Deactivated subject is excluded by EF global filter — not in result
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<SubjectSection>());

        var result = await _sut.GetCatalogAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        var item = result.Data!.First(i => i.SubjectId == subject.Id);
        item.Status.Should().Be(EnrollmentStatus.Bloqueada);
        item.MissingPrerequisites.Should().HaveCount(1);
        item.MissingPrerequisites[0].Name.Should().Be("[Materia inactiva]");
    }


    [Fact]
    public async Task EnrollAsync_WhenEnrollmentClosed_ShouldReturnFailure()
    {
        _uow.Setup(u => u.IsEnrollmentOpenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(Guid.NewGuid(), Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("selección de materias");
        _uow.Verify(u => u.AddSectionEnrollmentAsync(It.IsAny<SectionEnrollment>(), default), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_WhenEnrollmentOpen_ShouldProceedNormally()
    {
        _uow.Setup(u => u.IsEnrollmentOpenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var (student, subject, section) = SetupMatchingCareer(studentSemester: 1, subjectSemesterLevel: 1);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_SectionFull_ShouldReturnFailure()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 1);
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject, capacity: 1);

        // Fill the section to capacity
        var enrollment = new SectionEnrollment(Guid.NewGuid(), section.Id);
        TestDataBuilder.AddEnrollmentToSection(section, enrollment);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(student.Id, subject.Id, section.Id, default))
            .ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("capacidad");
    }


    [Fact]
    public async Task EnrollAsync_GeneralSubject_AnyCareerShouldSucceed()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.Ciberseguridad, semester: 1);
        var subject = TestDataBuilder.CreateSubject(
            code: "GEN-ELE1", career: string.Empty, semesterLevel: 1, appliesToAllCareers: true);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(student.Id, subject.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_SpecificSubject_WrongCareerShouldFail()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.Ciberseguridad, semester: 1);
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1, appliesToAllCareers: false);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("carrera");
    }

    [Fact]
    public async Task GetCatalogAsync_GeneralSubjectAppearsForAnyCareer()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.Ciberseguridad, semester: 1);
        var generalSub = TestDataBuilder.CreateSubject(
            code: "GEN-ELE1", career: string.Empty, semesterLevel: 1, appliesToAllCareers: true);
        var specificSub = TestDataBuilder.CreateSubject(
            code: "CS-SEC1", career: CareerTypes.Ciberseguridad, semesterLevel: 1);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSubjectsByCareerAsync(CareerTypes.Ciberseguridad, default))
            .ReturnsAsync(new[] { generalSub, specificSub });
        _subjectRepo.Setup(r => r.GetByStudentAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<SubjectSection>());

        var result = await _sut.GetCatalogAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        result.Data!.Should().Contain(i => i.SubjectId == generalSub.Id);
        result.Data!.Should().Contain(i => i.SubjectId == specificSub.Id);
    }


    [Fact]
    public async Task EnrollAsync_ExceedsMaxCredits_ShouldReturnFailure()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 3);

        // Target section: Lunes 08:00-10:00, 5 credits
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1, credits: 5);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject,
            dayOfWeek: DayOfWeekType.Lunes);

        // 5×4 + 2 = 22 credits; adding 5 would reach 27 > 24 — all on different days to avoid schedule conflict
        var days = new[] { DayOfWeekType.Martes, DayOfWeekType.Miercoles, DayOfWeekType.Jueves,
                           DayOfWeekType.Viernes, DayOfWeekType.Sabado };
        var enrolledSections = days.Select(day =>
            {
                var s = TestDataBuilder.CreateSubject(career: CareerTypes.IngenieriaEnSistemas, credits: 4);
                return TestDataBuilder.CreateSection(Guid.NewGuid(), s.Id, subject: s, dayOfWeek: day);
            })
            .Concat(new[]
            {
                TestDataBuilder.CreateSection(Guid.NewGuid(), null, dayOfWeek: DayOfWeekType.Domingo, subject:
                    TestDataBuilder.CreateSubject(career: CareerTypes.IngenieriaEnSistemas, credits: 2))
            })
            .ToList();

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(
            student.Id, subject.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(enrolledSections);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("créditos");
    }

    [Fact]
    public async Task EnrollAsync_AtExactMaxCredits_ShouldSucceed()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 1);

        // Target section: Lunes 08:00-10:00, 4 credits
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1, credits: 4);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject,
            dayOfWeek: DayOfWeekType.Lunes);

        // 5×4 = 20 credits already enrolled; adding 4 = 24 (exact limit, allowed)
        var days = new[] { DayOfWeekType.Martes, DayOfWeekType.Miercoles, DayOfWeekType.Jueves,
                           DayOfWeekType.Viernes, DayOfWeekType.Sabado };
        var enrolledSections = days.Select(day =>
            {
                var s = TestDataBuilder.CreateSubject(career: CareerTypes.IngenieriaEnSistemas, credits: 4);
                return TestDataBuilder.CreateSection(Guid.NewGuid(), s.Id, subject: s, dayOfWeek: day);
            })
            .ToList();

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(
            student.Id, subject.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(enrolledSections);

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }


    [Fact]
    public async Task EnrollAsync_ScheduleConflict_ShouldReturnFailureWithConflictInfo()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 1);

        var enrolledSubject = TestDataBuilder.CreateSubject(
            code: "IS-PRG1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var enrolledSection = TestDataBuilder.CreateSection(
            Guid.NewGuid(), enrolledSubject.Id, sectionCode: "B",
            dayOfWeek: DayOfWeekType.Lunes, subject: enrolledSubject);

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var section = TestDataBuilder.CreateSection(
            Guid.NewGuid(), subject.Id, sectionCode: "A",
            dayOfWeek: DayOfWeekType.Lunes, subject: subject);

        SetupStudentAndSection(student, section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(
            student.Id, subject.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(new[] { enrolledSection });

        var result = await _sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Choque de horario");
        result.ErrorMessage.Should().Contain("IS-PRG1");
        _uow.Verify(u => u.AddSectionEnrollmentAsync(It.IsAny<SectionEnrollment>(), default), Times.Never);
    }

    [Fact]
    public async Task GetCatalogAsync_ScheduleConflict_ShouldSetHasConflictFlag()
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 1);

        var subject = TestDataBuilder.CreateSubject(
            code: "IS-MAT1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var targetSection = TestDataBuilder.CreateSection(
            Guid.NewGuid(), subject.Id, sectionCode: "A",
            dayOfWeek: DayOfWeekType.Lunes, subject: subject);
        TestDataBuilder.AddSectionToSubject(subject, targetSection);

        var enrolledSubject = TestDataBuilder.CreateSubject(
            code: "IS-PRG1", career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1);
        var enrolledSection = TestDataBuilder.CreateSection(
            Guid.NewGuid(), enrolledSubject.Id, sectionCode: "B",
            dayOfWeek: DayOfWeekType.Lunes, subject: enrolledSubject);

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSubjectsByCareerAsync(CareerTypes.IngenieriaEnSistemas, default))
            .ReturnsAsync(new[] { subject });
        _subjectRepo.Setup(r => r.GetByStudentAsync(student.Id, default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(new[] { enrolledSection });

        var result = await _sut.GetCatalogAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        var item = result.Data!.First(i => i.SubjectId == subject.Id);
        item.HasScheduleConflict.Should().BeTrue();
        item.ConflictingWith.Should().Contain("IS-PRG1");
    }


    [Fact]
    public void AcademicSettings_DefaultMaxCreditsPerPeriod_Is24()
    {
        var settings = new AcademicSettings();
        settings.MaxCreditsPerPeriod.Should().Be(24);
    }

    [Fact]
    public async Task EnrollAsync_CustomMaxCredits_RejectsWhenExceeded()
    {
        // Limit = 18; student has 16 credits enrolled; new subject has 3 → 19 > 18 → reject
        var sut = CreateServiceWithLimit(18);

        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 1);
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1, credits: 3);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject,
            dayOfWeek: DayOfWeekType.Lunes);

        var enrolledSection = TestDataBuilder.CreateSection(
            Guid.NewGuid(), null, dayOfWeek: DayOfWeekType.Martes,
            subject: TestDataBuilder.CreateSubject(career: CareerTypes.IngenieriaEnSistemas, credits: 16));

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, default)).ReturnsAsync(section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(
            student.Id, subject.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(new[] { enrolledSection });

        var result = await sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("18");
        result.ErrorMessage.Should().Contain("créditos");
    }

    [Fact]
    public async Task EnrollAsync_CustomMaxCredits_AllowsAtExactLimit()
    {
        // Limit = 18; student has 15 credits enrolled; new subject has 3 → 15+3 = 18 = limit → allow
        var sut = CreateServiceWithLimit(18);

        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: 1);
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: 1, credits: 3);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject,
            dayOfWeek: DayOfWeekType.Lunes);

        var enrolledSection = TestDataBuilder.CreateSection(
            Guid.NewGuid(), null, dayOfWeek: DayOfWeekType.Martes,
            subject: TestDataBuilder.CreateSubject(career: CareerTypes.IngenieriaEnSistemas, credits: 15));

        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, default)).ReturnsAsync(section);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.IsEnrolledInAnyOtherSectionOfSubjectAsync(
            student.Id, subject.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetPrerequisitesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _subjectRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Enumerable.Empty<Subject>());
        _uow.Setup(u => u.GetEnrolledSectionsAsync(student.Id, default))
            .ReturnsAsync(new[] { enrolledSection });
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), default))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.CommitTransactionAsync(default)).Returns(Task.CompletedTask);

        var result = await sut.EnrollAsync(new EnrollRequestDto(student.Id, section.Id));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }


    private (Student student, Subject subject, SubjectSection section) SetupMatchingCareer(
        int studentSemester, int subjectSemesterLevel)
    {
        var student = TestDataBuilder.CreateStudentWithUser(
            career: CareerTypes.IngenieriaEnSistemas, semester: studentSemester);
        var subject = TestDataBuilder.CreateSubject(
            career: CareerTypes.IngenieriaEnSistemas, semesterLevel: subjectSemesterLevel);
        var section = TestDataBuilder.CreateSection(Guid.NewGuid(), subject.Id, subject: subject);

        SetupStudentAndSection(student, section);
        return (student, subject, section);
    }

    private void SetupStudentAndSection(Student student, SubjectSection section)
    {
        _studentRepo.Setup(r => r.GetWithGradesAsync(student.Id, default)).ReturnsAsync(student);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, default)).ReturnsAsync(section);
    }
}
