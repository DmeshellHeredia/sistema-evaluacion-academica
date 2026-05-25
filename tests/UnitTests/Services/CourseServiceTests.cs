using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Courses;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Constants;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class CourseServiceTests
{
    private readonly Mock<IUnitOfWork>        _uow         = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly Mock<IGradeRepository>   _gradeRepo   = new();
    private readonly CourseService _sut;

    public CourseServiceTests()
    {
        _uow.Setup(u => u.Students).Returns(_studentRepo.Object);
        _uow.Setup(u => u.Grades).Returns(_gradeRepo.Object);
        _sut = new CourseService(_uow.Object, NullLogger<CourseService>.Instance);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }


    [Fact]
    public async Task GetOverviewAsync_SectionNotFound_Returns404()
    {
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.GetOverviewAsync(Guid.NewGuid(), Guid.NewGuid(), Roles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetOverviewAsync_AdminOnAnySection_Returns200WithDto()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetOverviewAsync(section.Id, Guid.NewGuid(), Roles.Admin);

        result.IsSuccess.Should().BeTrue();
        result.Data!.SectionId.Should().Be(section.Id);
    }

    [Fact]
    public async Task GetOverviewAsync_ProfesorOwnSection_Returns200()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetOverviewAsync(section.Id, professorId, Roles.Profesor);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetOverviewAsync_ProfesorAlienSection_Returns403()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetOverviewAsync(section.Id, Guid.NewGuid(), Roles.Profesor);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetOverviewAsync_EstudianteEnrolled_Returns200()
    {
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.GetOverviewAsync(section.Id, studentId, Roles.Estudiante);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetOverviewAsync_EstudianteNotEnrolled_Returns403()
    {
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetOverviewAsync(section.Id, studentId, Roles.Estudiante);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }


    [Fact]
    public async Task GetAnnouncementsAsync_SectionNotFound_Returns404()
    {
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.GetAnnouncementsAsync(Guid.NewGuid(), Guid.NewGuid(), Roles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAnnouncementsAsync_AdminAuthorized_ReturnsMappedItems()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var announcement = BuildAnnouncement(section.Id, professorId);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.GetAnnouncementsBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { announcement });

        var result = await _sut.GetAnnouncementsAsync(section.Id, Guid.NewGuid(), Roles.Admin);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Title.Should().Be(announcement.Title);
    }

    [Fact]
    public async Task GetAnnouncementsAsync_ProfesorAlienSection_Returns403WithoutQueryingData()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetAnnouncementsAsync(section.Id, Guid.NewGuid(), Roles.Profesor);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.GetAnnouncementsBySectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task CreateAnnouncementAsync_ProfesorOwnSection_Returns201AndPersists()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var saved = BuildAnnouncement(section.Id, professorId);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.AddAnnouncementAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.GetAnnouncementByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var result = await _sut.CreateAnnouncementAsync(section.Id, new CreateAnnouncementDto("Título", "Contenido"), professorId);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _uow.Verify(u => u.AddAnnouncementAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_ProfesorAlienSection_Returns403WithoutPersisting()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.CreateAnnouncementAsync(section.Id, new CreateAnnouncementDto("T", "C"), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.AddAnnouncementAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task UpdateAnnouncementAsync_NotFound_Returns404()
    {
        _uow.Setup(u => u.GetAnnouncementByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Announcement?)null);

        var result = await _sut.UpdateAnnouncementAsync(Guid.NewGuid(), new UpdateAnnouncementDto("T", "C", true), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateAnnouncementAsync_NonAuthor_Returns403WithoutSaving()
    {
        var authorId = Guid.NewGuid();
        var announcement = BuildAnnouncement(Guid.NewGuid(), authorId);
        _uow.Setup(u => u.GetAnnouncementByIdAsync(announcement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(announcement);

        var result = await _sut.UpdateAnnouncementAsync(announcement.Id, new UpdateAnnouncementDto("T", "C", true), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAnnouncementAsync_Author_UpdatesAndSaves()
    {
        var authorId = Guid.NewGuid();
        var announcement = BuildAnnouncement(Guid.NewGuid(), authorId);
        _uow.Setup(u => u.GetAnnouncementByIdAsync(announcement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(announcement);

        var result = await _sut.UpdateAnnouncementAsync(announcement.Id, new UpdateAnnouncementDto("Nuevo Título", "Nuevo Contenido", false), authorId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Nuevo Título");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteAnnouncementAsync_NotFound_Returns404()
    {
        _uow.Setup(u => u.GetAnnouncementByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Announcement?)null);

        var result = await _sut.DeleteAnnouncementAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAnnouncementAsync_NonAuthor_Returns403WithoutSaving()
    {
        var authorId = Guid.NewGuid();
        var announcement = BuildAnnouncement(Guid.NewGuid(), authorId);
        _uow.Setup(u => u.GetAnnouncementByIdAsync(announcement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(announcement);

        var result = await _sut.DeleteAnnouncementAsync(announcement.Id, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAnnouncementAsync_Author_DeactivatesAndSaves()
    {
        var authorId = Guid.NewGuid();
        var announcement = BuildAnnouncement(Guid.NewGuid(), authorId);
        announcement.IsActive.Should().BeTrue();

        _uow.Setup(u => u.GetAnnouncementByIdAsync(announcement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(announcement);

        var result = await _sut.DeleteAnnouncementAsync(announcement.Id, authorId);

        result.IsSuccess.Should().BeTrue();
        announcement.IsActive.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetActivitiesAsync_SectionNotFound_Returns404()
    {
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.GetActivitiesAsync(Guid.NewGuid(), Guid.NewGuid(), Roles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetActivitiesAsync_ProfesorOwnSection_ReturnsMappedItems()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.GetActivitiesBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activity });

        var result = await _sut.GetActivitiesAsync(section.Id, professorId, Roles.Profesor);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Title.Should().Be(activity.Title);
    }

    [Fact]
    public async Task GetActivitiesAsync_ProfesorAlienSection_Returns403WithoutQueryingData()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetActivitiesAsync(section.Id, Guid.NewGuid(), Roles.Profesor);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.GetActivitiesBySectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task CreateActivityAsync_ProfesorAlienSection_Returns403WithoutPersisting()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.CreateActivityAsync(section.Id, new CreateActivityDto("T", "D", "Tarea", null, 10m, 0.2m), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.AddActivityAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateActivityAsync_InvalidActivityType_ReturnsFailureWithTypeName()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.CreateActivityAsync(section.Id, new CreateActivityDto("T", "D", "TipoInexistente", null, 10m, 0.2m), professorId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("TipoInexistente");
        _uow.Verify(u => u.AddActivityAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateActivityAsync_ProfesorOwnSection_ValidType_Returns201AndPersists()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.AddActivityAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.GetActivityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        var result = await _sut.CreateActivityAsync(section.Id, new CreateActivityDto("Tarea 1", "Desc", "Tarea", null, 10m, 0.2m), professorId);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _uow.Verify(u => u.AddActivityAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task SubmitAsync_ActivityNotFound_Returns404()
    {
        _uow.Setup(u => u.GetActivityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity?)null);

        var result = await _sut.SubmitAsync(Guid.NewGuid(), new SubmitDto("contenido"), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SubmitAsync_ActivityNotPublished_ReturnsNotAvailableFailure()
    {
        var sectionId = Guid.NewGuid();
        var activity = new Activity(sectionId, "Test", "Desc", ActivityType.Tarea, null, 10m, 0.2m, isPublished: false);
        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        var result = await _sut.SubmitAsync(activity.Id, new SubmitDto("contenido"), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("disponible");
        _uow.Verify(u => u.IsEnrolledInSectionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_StudentNotEnrolled_Returns403WithoutCreatingSubmission()
    {
        var sectionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var activity = new Activity(sectionId, "Test", "Desc", ActivityType.Tarea, null, 10m, 0.2m, isPublished: true);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.SubmitAsync(activity.Id, new SubmitDto("contenido"), studentId);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.AddSubmissionAsync(It.IsAny<ActivitySubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_StudentEnrolled_NewSubmission_PersistsAndReturnsDto()
    {
        var sectionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var activity = new Activity(sectionId, "Tarea 1", "Desc", ActivityType.Tarea, null, 10m, 0.2m, isPublished: true);
        var submission = new ActivitySubmission(activity.Id, student.Id, "mi respuesta");
        submission.Submit("mi respuesta");
        var enrolledStudent = TestDataBuilder.CreateStudentWithUser();
        TestDataBuilder.SetProperty(submission, "Student", enrolledStudent);
        TestDataBuilder.SetProperty(submission, "Activity", activity);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.SetupSequence(u => u.GetSubmissionAsync(activity.Id, student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivitySubmission?)null)  // pre-check: no existing submission
            .ReturnsAsync(submission);                // post-save: return persisted submission
        _uow.Setup(u => u.AddSubmissionAsync(It.IsAny<ActivitySubmission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.GetSubmissionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _sut.SubmitAsync(activity.Id, new SubmitDto("mi respuesta"), studentId);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.AddSubmissionAsync(It.IsAny<ActivitySubmission>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetActivitiesAsync_EstudianteEnrolled_Returns200WithItems()
    {
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var section = BuildFullSection(Guid.NewGuid());
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(u => u.GetActivitiesBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activity });

        var result = await _sut.GetActivitiesAsync(section.Id, studentId, Roles.Estudiante);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActivitiesAsync_EstudianteNotEnrolled_Returns403WithoutQueryingData()
    {
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var section = BuildFullSection(Guid.NewGuid());

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetActivitiesAsync(section.Id, studentId, Roles.Estudiante);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.GetActivitiesBySectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task CreateActivityAsync_RecursoType_ProfesorOwnSection_Returns201()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = new Activity(section.Id, "Guía PDF", "Recurso de apoyo", ActivityType.Recurso, null, 0m, 0m);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.AddActivityAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.GetActivityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        var result = await _sut.CreateActivityAsync(
            section.Id,
            new CreateActivityDto("Guía PDF", "Recurso de apoyo", "Recurso", null, 0m, 0m, true, "https://example.com/guia.pdf"),
            professorId);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Type.Should().Be("Recurso");
    }


    [Fact]
    public async Task UpdateActivityAsync_NotFound_Returns404()
    {
        _uow.Setup(u => u.GetActivityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity?)null);

        var result = await _sut.UpdateActivityAsync(Guid.NewGuid(), new UpdateActivityDto("T", "D", "Tarea", null, 10m, 0.2m, true, null), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateActivityAsync_ProfesorAlienSection_Returns403WithoutSaving()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.UpdateActivityAsync(activity.Id, new UpdateActivityDto("T", "D", "Tarea", null, 10m, 0.2m, true, null), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateActivityAsync_ProfesorOwnSection_UpdatesAndSaves()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.UpdateActivityAsync(activity.Id, new UpdateActivityDto("Título Nuevo", "Desc Nueva", "Tarea", null, 20m, 0.3m, false, null), professorId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Título Nuevo");
        result.Data.MaxScore.Should().Be(20m);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteActivityAsync_NotFound_Returns404()
    {
        _uow.Setup(u => u.GetActivityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity?)null);

        var result = await _sut.DeleteActivityAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteActivityAsync_ProfesorAlienSection_Returns403WithoutSaving()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.DeleteActivityAsync(activity.Id, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteActivityAsync_ProfesorOwnSection_DeactivatesAndSaves()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);
        activity.IsActive.Should().BeTrue();

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.DeleteActivityAsync(activity.Id, professorId);

        result.IsSuccess.Should().BeTrue();
        activity.IsActive.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetMySubmissionsAsync_StudentNotEnrolled_Returns403()
    {
        var sectionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);

        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetMySubmissionsAsync(sectionId, studentId);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.GetActivitiesBySectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMySubmissionsAsync_StudentEnrolled_ReturnsMappedItems()
    {
        var sectionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var activity = BuildActivity(sectionId);

        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(u => u.GetActivitiesBySectionAsync(sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activity });
        _uow.Setup(u => u.GetSubmissionsByStudentAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ActivitySubmission>());

        var result = await _sut.GetMySubmissionsAsync(sectionId, studentId);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().ActivityTitle.Should().Be(activity.Title);
    }


    [Fact]
    public async Task GradeSubmissionAsync_NotFound_Returns404()
    {
        _uow.Setup(u => u.GetSubmissionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivitySubmission?)null);

        var result = await _sut.GradeSubmissionAsync(Guid.NewGuid(), new GradeSubmissionDto(8m, null), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GradeSubmissionAsync_ProfesorAlienSection_Returns403WithoutSaving()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var (submission, activity) = BuildSubmissionWithActivity(section.Id);

        _uow.Setup(u => u.GetSubmissionByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GradeSubmissionAsync(submission.Id, new GradeSubmissionDto(8m, "Bien"), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GradeSubmissionAsync_ScoreExceedsMax_ReturnsFailure()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var (submission, activity) = BuildSubmissionWithActivity(section.Id, maxScore: 10m);

        _uow.Setup(u => u.GetSubmissionByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GradeSubmissionAsync(submission.Id, new GradeSubmissionDto(15m, null), professorId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("10");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GradeSubmissionAsync_ProfesorOwnSection_ValidScore_GradesAndSaves()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var (submission, activity) = BuildSubmissionWithActivity(section.Id, maxScore: 10m);

        _uow.Setup(u => u.GetSubmissionByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GradeSubmissionAsync(submission.Id, new GradeSubmissionDto(9m, "Excelente"), professorId);

        result.IsSuccess.Should().BeTrue();
        submission.Score.Should().Be(9m);
        submission.Status.Should().Be(SubmissionStatus.Calificada);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetSubmissionsForActivityAsync_ActivityNotFound_Returns404()
    {
        _uow.Setup(u => u.GetActivityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity?)null);

        var result = await _sut.GetSubmissionsForActivityAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetSubmissionsForActivityAsync_ProfesorAlienSection_Returns403()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetSubmissionsForActivityAsync(activity.Id, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSubmissionsForActivityAsync_ProfesorOwnSection_ReturnsMappedList()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var activity = BuildActivity(section.Id);
        var (submission, _) = BuildSubmissionWithActivity(section.Id);

        _uow.Setup(u => u.GetActivityByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.GetSubmissionsByActivityAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { submission });

        var result = await _sut.GetSubmissionsForActivityAsync(activity.Id, professorId);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }


    [Fact]
    public async Task GetGradeSuggestionsAsync_SectionNotFound_Returns404()
    {
        _uow.Setup(u => u.GetSectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSection?)null);

        var result = await _sut.GetGradeSuggestionsAsync(Guid.NewGuid(), Guid.NewGuid(), Roles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetGradeSuggestionsAsync_ProfesorAlienSection_Returns403()
    {
        var section = BuildFullSection(Guid.NewGuid());
        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);

        var result = await _sut.GetGradeSuggestionsAsync(section.Id, Guid.NewGuid(), Roles.Profesor);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGradeSuggestionsAsync_EstudianteRole_Returns403()
    {
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var section = BuildFullSection(Guid.NewGuid());

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.GetGradeSuggestionsAsync(section.Id, studentId, Roles.Estudiante);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGradeSuggestionsAsync_NoActivities_SuggestedScoreIsNull()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var student = TestDataBuilder.CreateStudentWithUser();

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.GetStudentsBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { student });
        _uow.Setup(u => u.GetActivitiesBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Activity>());
        _gradeRepo.Setup(r => r.GetBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Grade>());

        var result = await _sut.GetGradeSuggestionsAsync(section.Id, professorId, Roles.Profesor);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().SuggestedScore.Should().BeNull();
    }

    [Fact]
    public async Task GetGradeSuggestionsAsync_AllActivitiesGraded_ReturnsCorrectWeightedScore()
    {
        var professorId = Guid.NewGuid();
        var section = BuildFullSection(professorId);
        var student = TestDataBuilder.CreateStudentWithUser();
        var activity = new Activity(section.Id, "Tarea 1", "Desc", ActivityType.Tarea, null, 10m, 20m, isPublished: true);
        var submission = new ActivitySubmission(activity.Id, student.Id, "respuesta");
        submission.Submit("respuesta");
        submission.Grade(8m, null);
        TestDataBuilder.SetProperty(activity, "Submissions", new List<ActivitySubmission> { submission });
        var grade = TestDataBuilder.CreateGrade(studentId: student.Id, sectionId: section.Id, value: 9m);

        _uow.Setup(u => u.GetSectionByIdAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(section);
        _uow.Setup(u => u.GetStudentsBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { student });
        _uow.Setup(u => u.GetActivitiesBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activity });
        _gradeRepo.Setup(r => r.GetBySectionAsync(section.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { grade });

        var result = await _sut.GetGradeSuggestionsAsync(section.Id, professorId, Roles.Profesor);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Data!.First();
        dto.SuggestedScore.Should().Be(8.00m);   // (8/10 * 20) / 20 * 10 = 8.00
        dto.OfficialGrade.Should().Be(9m);
        dto.CompletedActivities.Should().Be(1);
        dto.TotalActivities.Should().Be(1);
    }


    [Fact]
    public async Task GetMyGradeSuggestionAsync_StudentNotEnrolled_Returns403()
    {
        var sectionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);

        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetMyGradeSuggestionAsync(sectionId, studentId);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetMyGradeSuggestionAsync_NoGradedActivities_SuggestedScoreIsNull()
    {
        var sectionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = TestDataBuilder.CreateStudent(userId: studentId);
        var activity = BuildActivity(sectionId);

        _studentRepo.Setup(r => r.GetByUserIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _uow.Setup(u => u.IsEnrolledInSectionAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(u => u.GetActivitiesBySectionAsync(sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activity });
        _uow.Setup(u => u.GetSubmissionsByStudentAsync(student.Id, sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ActivitySubmission>());
        _gradeRepo.Setup(r => r.GetBySectionAsync(sectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Grade>());

        var result = await _sut.GetMyGradeSuggestionAsync(sectionId, studentId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.SuggestedScore.Should().BeNull();
        result.Data.OfficialGrade.Should().BeNull();
        result.Data.TotalActivities.Should().Be(1);
    }


    private SubjectSection BuildFullSection(Guid professorId)
    {
        var professor = TestDataBuilder.CreateProfesorUser();
        var subject = TestDataBuilder.CreateSubject();
        var section = TestDataBuilder.CreateSection(professorId, subject.Id, subject: subject);
        TestDataBuilder.SetProperty(section, "Professor", professor);
        return section;
    }

    private static Announcement BuildAnnouncement(Guid sectionId, Guid authorId)
    {
        var author = TestDataBuilder.CreateProfesorUser();
        var a = new Announcement(sectionId, authorId, "Anuncio de prueba", "Contenido del anuncio", true);
        TestDataBuilder.SetProperty(a, "Author", author);
        return a;
    }

    private static Activity BuildActivity(Guid sectionId) =>
        new(sectionId, "Tarea 1", "Descripción de la tarea", ActivityType.Tarea, null, 10m, 0.2m, isPublished: true);

    private static (ActivitySubmission submission, Activity activity) BuildSubmissionWithActivity(
        Guid sectionId, decimal maxScore = 10m)
    {
        var activity = new Activity(sectionId, "Tarea", "Desc", ActivityType.Tarea, null, maxScore, 0.2m, isPublished: true);
        var student = TestDataBuilder.CreateStudentWithUser();
        var submission = new ActivitySubmission(activity.Id, student.Id, "respuesta");
        submission.Submit("respuesta");
        TestDataBuilder.SetProperty(submission, "Student", student);
        TestDataBuilder.SetProperty(submission, "Activity", activity);
        return (submission, activity);
    }
}
