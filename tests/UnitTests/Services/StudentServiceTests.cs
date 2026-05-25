using FluentAssertions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class StudentServiceTests
{
    private static readonly Guid EstudianteRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private readonly Mock<IUnitOfWork>        _uow            = new();
    private readonly Mock<IStudentRepository> _studentRepo    = new();
    private readonly Mock<IUserRepository>    _userRepo       = new();
    private readonly Mock<IRoleRepository>    _roleRepo       = new();
    private readonly Mock<IPasswordHasher>    _passwordHasher = new();
    private readonly StudentService           _sut;

    public StudentServiceTests()
    {
        var studentRole = new Role(RoleType.Estudiante, "Estudiante", "");
        TestDataBuilder.SetProperty(studentRole, "Id", EstudianteRoleId);

        _uow.Setup(u => u.Students).Returns(_studentRepo.Object);
        _uow.Setup(u => u.Users).Returns(_userRepo.Object);
        _uow.Setup(u => u.Roles).Returns(_roleRepo.Object);
        _roleRepo.Setup(r => r.GetByTypeAsync(RoleType.Estudiante, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentRole);

        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_pass");
        _sut = new StudentService(_uow.Object, _passwordHasher.Object);
    }


    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResult()
    {
        var students = new[]
        {
            TestDataBuilder.CreateStudentWithUser("EST-2025-0001"),
            TestDataBuilder.CreateStudentWithUser("EST-2025-0002", email: "maria@academia.com",
                                                  firstName: "María", lastName: "López"),
        };

        _studentRepo
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((students, 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoStudents_ShouldReturnEmptyPagedResult()
    {
        _studentRepo
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Student>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_PassesSearchTermToRepository()
    {
        _studentRepo
            .Setup(r => r.GetPagedAsync(1, 10, "garcia", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Student>(), 0));

        var result = await _sut.GetAllAsync(1, 10, "garcia");

        result.IsSuccess.Should().BeTrue();
        _studentRepo.Verify(
            r => r.GetPagedAsync(1, 10, "garcia", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_ReturnsOnlyMatchingStudents()
    {
        var match = TestDataBuilder.CreateStudentWithUser("EST-2025-0002",
            email: "garcia@academia.com", firstName: "Carlos", lastName: "García");

        _studentRepo
            .Setup(r => r.GetPagedAsync(1, 10, "garcia", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { match }, 1));

        var result = await _sut.GetAllAsync(1, 10, "garcia");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().Email.Should().Be("garcia@academia.com");
    }


    [Fact]
    public async Task GetByIdAsync_WhenStudentExists_ShouldReturnStudentDto()
    {
        var student = TestDataBuilder.CreateStudentWithUser("EST-2025-0001");

        _studentRepo
            .Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var result = await _sut.GetByIdAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StudentCode.Should().Be("EST-2025-0001");
        result.Data.Career.Should().Be("Ingeniería en Sistemas");
    }

    [Fact]
    public async Task GetByIdAsync_WhenStudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo
            .Setup(r => r.GetWithGradesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("Estudiante");
    }


    [Fact]
    public async Task CreateAsync_WhenDerivedEmailAlreadyExists_ShouldReturnFailure()
    {
        // Derived email for "Juan"/"Pérez" → "juan.perez@academia.com"
        _userRepo
            .Setup(r => r.EmailExistsAsync("juan.perez@academia.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto    = new CreateStudentDto("Pass123!", "Juan", "Pérez", "CS", 1);
        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("correo");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_HappyPath_ShouldCreateUserWithDerivedEmail()
    {
        _userRepo
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));
        _studentRepo
            .Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _studentRepo
            .Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Returns<Student, CancellationToken>((s, _) => Task.FromResult(s));
        _uow
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // "Ana"/"García" → derived email "ana.garcia@academia.com"
        var dto    = new CreateStudentDto("Pass123!", "Ana", "García", "Derecho", 2);
        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);

        _userRepo.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "ana.garcia@academia.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _studentRepo.Verify(r => r.AddAsync(
            It.Is<Student>(s => s.Career == "Derecho" && s.Semester == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldDeriveEmailFromNameIgnoringAccents()
    {
        _userRepo
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));
        _studentRepo.Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Returns<Student, CancellationToken>((s, _) => Task.FromResult(s));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // "María"/"Gómez" → derived email "maria.gomez@academia.com"
        await _sut.CreateAsync(new CreateStudentDto("Pass123!", "María", "Gómez", "CS", 1));

        _userRepo.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "maria.gomez@academia.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateStudentCodeWithYearAndRandomHex()
    {
        _userRepo
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));
        _studentRepo.Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Returns<Student, CancellationToken>((s, _) => Task.FromResult(s));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CreateAsync(new CreateStudentDto("P123!", "Pedro", "Lopez", "CS", 1));

        var year = DateTime.UtcNow.Year;
        _studentRepo.Verify(r => r.AddAsync(
            It.Is<Student>(s =>
                s.StudentCode.StartsWith($"EST-{year}-") &&
                System.Text.RegularExpressions.Regex.IsMatch(
                    s.StudentCode, @$"^EST-{year}-[0-9A-F]{{6}}$")),
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task UpdateAsync_WhenStudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateStudentDto("CS", 3));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateAsync_HappyPath_ShouldUpdateCareerAndSemester()
    {
        var student = TestDataBuilder.CreateStudentWithUser(career: "Administración", semester: 2);

        _studentRepo.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _studentRepo.Setup(r => r.UpdateAsync(student, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(student.Id, new UpdateStudentDto("Ingeniería Civil", 6));

        result.IsSuccess.Should().BeTrue();
        _studentRepo.Verify(r => r.UpdateAsync(
            It.Is<Student>(s => s.Career == "Ingeniería Civil" && s.Semester == 6),
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteAsync_WhenStudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_HappyPath_ShouldSoftDeleteStudent()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        _studentRepo.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _studentRepo.Setup(r => r.UpdateAsync(student, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        student.IsActive.Should().BeFalse();   // verifica soft-delete en la entidad
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetGradeReportAsync_WhenStudentNotFound_ShouldReturnNotFound()
    {
        _studentRepo
            .Setup(r => r.GetWithGradesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _sut.GetGradeReportAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetGradeReportAsync_ShouldReturnReportWithOverallAverage()
    {
        var student = TestDataBuilder.CreateStudentWithUser();
        var subject = TestDataBuilder.CreateSubject();
        var grade1  = TestDataBuilder.CreateGradeWithNavigation(student, subject, 8.0m);
        var grade2  = TestDataBuilder.CreateGradeWithNavigation(student, subject, 9.0m, "2025-2");
        TestDataBuilder.AddGrades(student, grade1, grade2);

        _studentRepo
            .Setup(r => r.GetWithGradesAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var gradeRepo = new Mock<IGradeRepository>();
        gradeRepo.Setup(r => r.GetByStudentWithDetailsAsync(student.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { grade1, grade2 });
        _uow.Setup(u => u.Grades).Returns(gradeRepo.Object);

        var result = await _sut.GetGradeReportAsync(student.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StudentCode.Should().Be(student.StudentCode);
        result.Data.OverallAverage.Should().Be(8.5m);   // (8 + 9) / 2
        result.Data.SubjectGrades.Should().HaveCount(2);
    }


    [Fact]
    public async Task GetByUserIdAsync_WhenStudentExists_ShouldReturnDto()
    {
        var student = TestDataBuilder.CreateStudentWithUser("EST-2025-0001");
        var userId  = student.UserId;

        _studentRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var result = await _sut.GetByUserIdAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Data!.StudentCode.Should().Be("EST-2025-0001");
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoStudentForUser_ShouldReturnNotFound()
    {
        _studentRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _sut.GetByUserIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
